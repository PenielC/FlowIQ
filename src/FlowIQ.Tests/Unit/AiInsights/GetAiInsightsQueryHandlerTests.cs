using AwesomeAssertions;
using FlowIQ.Application.AiInsights.Queries.GetAiInsights;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.CashFlowForecasting;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.AiInsights;

public class GetAiInsightsQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IPlannedOwnerDrawRepository> _drawRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Today = new(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime HistoryStart = Today.AddDays(-29);

    private static Transaction MakeTransaction(Guid companyId, DateTime dateUtc, TransactionCategory category, decimal amount) =>
        new(companyId, "Test", category, amount, dateUtc, TransactionStatus.Completed, "USD", amount, 1m);

    public GetAiInsightsQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);
    }

    /// <summary>A company whose balance was <paramref name="baseline"/> 30 days ago, plus <paramref name="history"/> since.</summary>
    private void SetupDefaults(Guid companyId, decimal baseline = 1000m, List<Transaction>? history = null, List<PlannedOwnerDraw>? draws = null)
    {
        _companyRepository.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(new Company("Test"));
        _transactionRepository
            .Setup(r => r.GetBalanceBeforeDateAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, HistoryStart, Today.AddDays(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history ?? []);
        _drawRepository.Setup(r => r.ListByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(draws ?? []);
        _invoiceRepository
            .Setup(r => r.GetPendingDueBetweenAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice>());
        _invoiceRepository
            .Setup(r => r.GetAtRiskSummaryAsync(companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtRiskSummary(0, 0m));
    }

    private GetAiInsightsQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _invoiceRepository.Object, _companyRepository.Object,
            new CashFlowForecaster(_transactionRepository.Object, _invoiceRepository.Object, _drawRepository.Object, _dateTimeProvider.Object),
            _dateTimeProvider.Object);

    [Fact]
    public async Task Handle_BalanceGrew_AddsPositiveForecastInsight()
    {
        var companyId = Guid.NewGuid();
        // 1000 -> 1200 over 30 days; the same trend again gives 1400 in 30 days: +200 / 1200.
        SetupDefaults(companyId, 1000m, [MakeTransaction(companyId, Today.AddDays(-5), TransactionCategory.Sales, 200m)]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var forecastInsight = result.Insights.Single(i => i.Title.Contains("forecasted"));
        forecastInsight.Tone.Should().Be(InsightTone.Positive);
        forecastInsight.Title.Should().Contain("increase");
        forecastInsight.Title.Should().Contain("16.7%");
    }

    [Fact]
    public async Task Handle_BalanceShrank_AddsWarningForecastInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId, 1000m, [MakeTransaction(companyId, Today.AddDays(-5), TransactionCategory.OperatingExpense, -200m)]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var forecastInsight = result.Insights.Single(i => i.Title.Contains("forecasted"));
        forecastInsight.Tone.Should().Be(InsightTone.Warning);
        forecastInsight.Title.Should().Contain("decrease");
    }

    [Fact]
    public async Task Handle_NoNetChange_OmitsForecastInsight()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Should().NotContain(i => i.Title.Contains("forecasted"));
    }

    [Fact]
    public async Task Handle_ZeroCurrentBalance_OmitsForecastInsight_NoDivideByZero()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId, 500m, [MakeTransaction(companyId, Today.AddDays(-3), TransactionCategory.OperatingExpense, -500m)]);

        var act = () => CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
        var result = await act();
        result.Insights.Should().NotContain(i => i.Title.Contains("forecasted"));
    }

    [Fact]
    public async Task Handle_PlannedDrawTakesBalanceBelowZero_WarnsOfSqueezeOnThatDay()
    {
        var companyId = Guid.NewGuid();
        var schoolFees = new PlannedOwnerDraw(companyId, "School fees", 500m, "USD", 500m, 1m, DrawFrequency.Once, Today.AddDays(10), null);
        SetupDefaults(companyId, 100m, draws: [schoolFees]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var squeeze = result.Insights.Single(i => i.Title.Contains("run short"));
        squeeze.Tone.Should().Be(InsightTone.Warning);
        squeeze.Title.Should().Contain("Oct 3");
        squeeze.Description.Should().Contain("USD -400.00");
    }

    [Fact]
    public async Task Handle_DrawWithinTwoWeeks_MentionsIt()
    {
        var companyId = Guid.NewGuid();
        var rent = new PlannedOwnerDraw(companyId, "Home rent", 300m, "USD", 300m, 1m, DrawFrequency.Monthly, Today.AddDays(7), null);
        SetupDefaults(companyId, 5000m, draws: [rent]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        var insight = result.Insights.Single(i => i.Title.StartsWith("Home rent"));
        insight.Title.Should().Contain("USD 300.00").And.Contain("Sep 30");
        result.Insights.Should().NotContain(i => i.Title.Contains("run short"));
    }

    [Fact]
    public async Task Handle_OwnerDrawingsGrew_IsNotFlaggedAsSpendingSpike()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var thisMonthStart = new DateTime(Today.Year, Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, thisMonthStart, thisMonthStart.AddMonths(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, thisMonthStart.AddDays(2), TransactionCategory.OwnerDrawings, -900m)]);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, lastMonthStart, thisMonthStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTransaction(companyId, lastMonthStart.AddDays(2), TransactionCategory.OwnerDrawings, -100m)]);

        var result = await CreateHandler().Handle(new GetAiInsightsQuery(companyId), CancellationToken.None);

        result.Insights.Should().NotContain(i => i.Title.Contains("spending increased"));
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
