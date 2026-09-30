using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.CashFlowForecasting;
using FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.Invoicing;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CashFlowForecasting;

public class GetCashFlowForecastQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IPlannedOwnerDrawRepository> _drawRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private static readonly DateTime Today = new(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);

    public GetCashFlowForecastQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);
        _drawRepository.Setup(r => r.ListByCompanyAsync(_companyId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _invoiceRepository
            .Setup(r => r.GetPendingDueBetweenAsync(_companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice>());
    }

    private GetCashFlowForecastQueryHandler CreateHandler() =>
        new(new CashFlowForecaster(_transactionRepository.Object, _invoiceRepository.Object, _drawRepository.Object, _dateTimeProvider.Object));

    private Transaction MakeTransaction(DateTime dateUtc, decimal amount, TransactionCategory category = TransactionCategory.Other) =>
        new(_companyId, "Test", category, amount, dateUtc, TransactionStatus.Completed, "USD", amount, 1m);

    private Invoice MakeInvoice(decimal amount, DateTime dueUtc) =>
        new(_companyId, "Chipo", [("Catering", amount)], Today.AddDays(-40), dueUtc, InvoiceStatus.Sent, "USD", 1m, null);

    private void SetupHistory(decimal baseline, params Transaction[] transactions)
    {
        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(_companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(_companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.ToList());
    }

    private static decimal? ForecastOn(CashFlowForecast forecast, DateTime day) => forecast.Points.Single(p => p.DateUtc == day).Forecast;

    [Fact]
    public async Task Handle_WithNoTransactions_ProjectsFlatBaselineForward()
    {
        SetupHistory(1000m);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 10, ForecastDays: 5), CancellationToken.None);

        result.Points.Should().HaveCount(15); // 10 history + 5 forecast
        result.Points.Last().Forecast.Should().Be(1000m);
        result.CurrentBalance.Should().Be(1000m);
        result.LowestBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_WithConsistentDailyGain_ProjectsTheTrendForward()
    {
        var historyStart = Today.AddDays(-2); // 3-day history window
        SetupHistory(0m,
            MakeTransaction(historyStart, 100m),
            MakeTransaction(historyStart.AddDays(1), 100m),
            MakeTransaction(historyStart.AddDays(2), 100m));

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 3, ForecastDays: 2), CancellationToken.None);

        result.Points.Should().HaveCount(5);
        var today = result.Points.First(p => p.DateUtc == Today);
        today.Actual.Should().Be(300m);
        today.Forecast.Should().Be(300m); // connects the dashed line to the solid one
        ForecastOn(result, Today.AddDays(1)).Should().Be(400m);
        ForecastOn(result, Today.AddDays(2)).Should().Be(500m);
    }

    [Fact]
    public async Task Handle_HistoryPointsHaveNullForecast_ExceptTheLastOne()
    {
        SetupHistory(0m);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 5, ForecastDays: 3), CancellationToken.None);

        result.Points.Where(p => p.DateUtc < Today).Should().OnlyContain(p => p.Forecast == null && p.Actual != null);
        result.Points.Where(p => p.DateUtc > Today).Should().OnlyContain(p => p.Actual == null && p.Forecast != null);
    }

    [Fact]
    public async Task Handle_PlannedDraw_DipsTheBalanceOnItsDate_InsteadOfSmoothingIt()
    {
        SetupHistory(1000m);
        var fees = new PlannedOwnerDraw(_companyId, "School fees", 600m, "USD", 600m, 1m, DrawFrequency.Once, Today.AddDays(3), null);
        _drawRepository.Setup(r => r.ListByCompanyAsync(_companyId, It.IsAny<CancellationToken>())).ReturnsAsync([fees]);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 10, ForecastDays: 10), CancellationToken.None);

        ForecastOn(result, Today.AddDays(2)).Should().Be(1000m);
        ForecastOn(result, Today.AddDays(3)).Should().Be(400m);
        result.Events.Should().ContainSingle(e => e.Kind == ForecastEventKind.OwnerDraw && e.Amount == -600m && e.Label == "School fees");
        result.LowestBalance.Should().Be(400m);
        result.LowestBalanceDateUtc.Should().Be(Today.AddDays(3));
    }

    [Fact]
    public async Task Handle_PastDrawings_LeftOutOfTrend_WhenDrawsArePlanned()
    {
        SetupHistory(1000m, MakeTransaction(Today.AddDays(-1), -100m, TransactionCategory.OwnerDrawings));
        var rent = new PlannedOwnerDraw(_companyId, "Home rent", 50m, "USD", 50m, 1m, DrawFrequency.Once, Today.AddDays(40), null);
        _drawRepository.Setup(r => r.ListByCompanyAsync(_companyId, It.IsAny<CancellationToken>())).ReturnsAsync([rent]);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 10, ForecastDays: 10), CancellationToken.None);

        result.CurrentBalance.Should().Be(900m);
        result.Points.Last().Forecast.Should().Be(900m); // the planned draw is outside the window; no smoothed drawing trend
    }

    [Fact]
    public async Task Handle_PastDrawings_StayInTrend_WhenNothingIsPlanned()
    {
        SetupHistory(1000m, MakeTransaction(Today.AddDays(-1), -100m, TransactionCategory.OwnerDrawings));

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 10, ForecastDays: 10), CancellationToken.None);

        result.Points.Last().Forecast.Should().Be(800m); // -10/day for 10 days
    }

    [Fact]
    public async Task Handle_OwnerContribution_IsNotProjectedToRecur()
    {
        SetupHistory(1000m, MakeTransaction(Today.AddDays(-1), 500m, TransactionCategory.OwnerContribution));

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 10, ForecastDays: 10), CancellationToken.None);

        result.Points.Last().Forecast.Should().Be(1500m);
    }

    [Fact]
    public async Task Handle_PendingInvoice_ArrivesOnItsDueDate_AndPaidInvoicesDoNotAlsoTrend()
    {
        var paid = Transaction.ForPaidInvoice(MakeInvoice(300m, Today.AddDays(-5)), Today.AddDays(-2));
        SetupHistory(1000m, paid);
        _invoiceRepository
            .Setup(r => r.GetPendingDueBetweenAsync(_companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeInvoice(250m, Today.AddDays(5))]);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 10, ForecastDays: 10), CancellationToken.None);

        ForecastOn(result, Today.AddDays(4)).Should().Be(1300m);
        ForecastOn(result, Today.AddDays(5)).Should().Be(1550m);
        result.Points.Last().Forecast.Should().Be(1550m);
        result.Events.Should().ContainSingle(e => e.Kind == ForecastEventKind.InvoiceDue && e.Label == "Chipo");
    }

    [Fact]
    public async Task Handle_BeyondTheInvoiceHorizon_AverageInvoiceIncomeTakesOver()
    {
        var paid = Transaction.ForPaidInvoice(MakeInvoice(300m, Today.AddDays(-5)), Today.AddDays(-2));
        SetupHistory(1000m, paid);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(_companyId, HistoryDays: 30, ForecastDays: 32), CancellationToken.None);

        ForecastOn(result, Today.AddDays(30)).Should().Be(1300m);
        ForecastOn(result, Today.AddDays(32)).Should().Be(1320m); // +10/day after day 30
    }
}
