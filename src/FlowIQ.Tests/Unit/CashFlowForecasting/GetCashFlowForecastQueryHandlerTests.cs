using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CashFlowForecasting;

public class GetCashFlowForecastQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Today = new(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);

    private GetCashFlowForecastQueryHandler CreateHandler() => new(_transactionRepository.Object, _dateTimeProvider.Object);

    private static Transaction MakeTransaction(Guid companyId, DateTime dateUtc, decimal amount) =>
        new(companyId, "Test", TransactionCategory.Other, amount, dateUtc, TransactionStatus.Completed, "USD", amount, 1m);

    [Fact]
    public async Task Handle_WithNoTransactions_ProjectsFlatBaselineForward()
    {
        var companyId = Guid.NewGuid();
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);
        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000m);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(companyId, HistoryDays: 10, ForecastDays: 5), CancellationToken.None);

        result.Points.Should().HaveCount(15); // 10 history + 5 forecast
        result.Points.Should().OnlyContain(p => p.Actual == 1000m || p.Forecast == 1000m || p.Forecast == null);
        result.Points.Last().Forecast.Should().Be(1000m); // no net change → flat projection
    }

    [Fact]
    public async Task Handle_WithConsistentDailyGain_ProjectsTheTrendForward()
    {
        var companyId = Guid.NewGuid();
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);

        var historyStart = Today.AddDays(-2); // 3-day history window
        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(companyId, historyStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // +100 net each of the 3 history days
        var transactions = new List<Transaction>
        {
            MakeTransaction(companyId, historyStart, 100m),
            MakeTransaction(companyId, historyStart.AddDays(1), 100m),
            MakeTransaction(companyId, historyStart.AddDays(2), 100m),
        };
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, historyStart, Today.AddDays(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(companyId, HistoryDays: 3, ForecastDays: 2), CancellationToken.None);

        result.Points.Should().HaveCount(5); // 3 history + 2 forecast

        var lastActualPoint = result.Points.First(p => p.DateUtc == Today);
        lastActualPoint.Actual.Should().Be(300m);
        lastActualPoint.Forecast.Should().Be(300m); // connects the dashed line to the solid one

        var forecastPoints = result.Points.Where(p => p.DateUtc > Today).OrderBy(p => p.DateUtc).ToList();
        forecastPoints[0].Forecast.Should().Be(400m); // 300 + avg daily gain of 100
        forecastPoints[1].Forecast.Should().Be(500m);
        forecastPoints.Should().OnlyContain(p => p.Actual == null);
    }

    [Fact]
    public async Task Handle_HistoryPointsHaveNullForecast_ExceptTheLastOne()
    {
        var companyId = Guid.NewGuid();
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);
        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetCashFlowForecastQuery(companyId, HistoryDays: 5, ForecastDays: 3), CancellationToken.None);

        var historyPoints = result.Points.Where(p => p.DateUtc < Today).ToList();
        historyPoints.Should().OnlyContain(p => p.Forecast == null && p.Actual != null);

        var futurePoints = result.Points.Where(p => p.DateUtc > Today).ToList();
        futurePoints.Should().OnlyContain(p => p.Actual == null && p.Forecast != null);
    }
}
