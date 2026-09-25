using AwesomeAssertions;
using FlowIQ.Application.AiInsights.Queries.GetAiInsights;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.AiInsights;

public class GetAiInsightsQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Today = new(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);

    private static Transaction MakeTransaction(Guid companyId, DateTime dateUtc, TransactionCategory category, decimal amount) =>
        new(companyId, "Test", category, amount, dateUtc, TransactionStatus.Completed, "USD", amount, 1m);

    public GetAiInsightsQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);
    }

    private void SetupDefaults(Guid companyId, decimal currentBalance = 1000m, decimal baseline = 1000m)
    {
        _transactionRepository.Setup(r => r.GetBalanceAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(currentBalance);
        _transactionRepository
            .Setup(r => r.GetBalanceBeforeDateAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _invoiceRepository
            .Setup(r => r.GetAtRiskSummaryAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtRiskSummary(0, 0m));
    }

    private GetAiInsightsQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _invoiceRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task Handle_BalanceGrew_AddsPositiveForecastInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId, currentBalance: 1200m, baseline: 1000m); // +20% net change

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var forecastInsight = result.Insights.Single(i => i.Title.Contains("forecasted"));
        forecastInsight.Tone.Should().Be(InsightTone.Positive);
        forecastInsight.Title.Should().Contain("increase");
        forecastInsight.Title.Should().Contain("16.7%"); // 200/1200 * 100 rounded
    }

    [Fact]
    public async Task Handle_BalanceShrank_AddsWarningForecastInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId, currentBalance: 800m, baseline: 1000m);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var forecastInsight = result.Insights.Single(i => i.Title.Contains("forecasted"));
        forecastInsight.Tone.Should().Be(InsightTone.Warning);
        forecastInsight.Title.Should().Contain("decrease");
    }

    [Fact]
    public async Task Handle_NoNetChange_OmitsForecastInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId, currentBalance: 1000m, baseline: 1000m);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Should().NotContain(i => i.Title.Contains("forecasted"));
    }

    [Fact]
    public async Task Handle_ZeroCurrentBalance_OmitsForecastInsight_NoDivideByZero()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId, currentBalance: 0m, baseline: 500m);

        var act = () => CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
        var result = await act();
        result.Insights.Should().NotContain(i => i.Title.Contains("forecasted"));
    }

    [Fact]
    public async Task Handle_NoAtRiskInvoices_OmitsAtRiskInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Should().NotContain(i => i.Title.Contains("at risk"));
    }

    [Fact]
    public async Task Handle_AtRiskInvoicesPresent_AddsWarningInsightWithCountAndTotal()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);
        _invoiceRepository
            .Setup(r => r.GetAtRiskSummaryAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtRiskSummary(2, 4200m));

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var insight = result.Insights.Single(i => i.Title.Contains("at risk"));
        insight.Tone.Should().Be(InsightTone.Warning);
        insight.Title.Should().Contain("2 customers are at risk");
        insight.Description.Should().Contain("4,200.00");
    }

    [Fact]
    public async Task Handle_SingleAtRiskInvoice_UsesSingularGrammar()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);
        _invoiceRepository
            .Setup(r => r.GetAtRiskSummaryAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtRiskSummary(1, 500m));

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Single(i => i.Title.Contains("at risk")).Title.Should().Contain("1 customer is at risk");
    }

    [Fact]
    public async Task Handle_CategorySpendingSpike_AddsCautionInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var thisMonthStart = new DateTime(Today.Year, Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, thisMonthStart, thisMonthStart.AddMonths(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, thisMonthStart.AddDays(2), TransactionCategory.Utilities, -100m)]);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, lastMonthStart, thisMonthStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, lastMonthStart.AddDays(2), TransactionCategory.Utilities, -50m)]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var insight = result.Insights.Single(i => i.Title.Contains("spending increased"));
        insight.Tone.Should().Be(InsightTone.Caution);
        insight.Title.Should().Contain("Utilities");
        insight.Title.Should().Contain("100%");
    }

    [Fact]
    public async Task Handle_NoBaselineSpendLastMonth_DoesNotFlagAsSpike()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var thisMonthStart = new DateTime(Today.Year, Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, thisMonthStart, thisMonthStart.AddMonths(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, thisMonthStart.AddDays(2), TransactionCategory.Utilities, -100m)]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Should().NotContain(i => i.Title.Contains("spending increased"));
    }

    [Fact]
    public async Task Handle_SmallSpendBelowThreshold_DoesNotFlagAsSpike()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var thisMonthStart = new DateTime(Today.Year, Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, thisMonthStart, thisMonthStart.AddMonths(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, thisMonthStart.AddDays(2), TransactionCategory.Utilities, -10m)]);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, lastMonthStart, thisMonthStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, lastMonthStart.AddDays(2), TransactionCategory.Utilities, -1m)]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Should().NotContain(i => i.Title.Contains("spending increased"));
    }
}
