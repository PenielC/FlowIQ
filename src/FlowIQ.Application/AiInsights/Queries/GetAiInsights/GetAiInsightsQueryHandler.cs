using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.CashFlowForecasting;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using Mediator;

namespace FlowIQ.Application.AiInsights.Queries.GetAiInsights;

/// <summary>
/// Deterministic, rule-based insights computed from real transaction/invoice data —
/// not a call to an external AI/ML service (same honest-implementation approach as
/// CashFlowForecasting and AiCategorisation).
/// </summary>
public class GetAiInsightsQueryHandler(
    ITransactionRepository transactionRepository,
    IInvoiceRepository invoiceRepository,
    IRepository<Company> companyRepository,
    CashFlowForecaster forecaster,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetAiInsightsQuery, AiInsightsResult>
{
    private const int HistoryDays = 30;
    private const int UpcomingDrawWindowDays = 14;
    private const int AtRiskWindowDays = 7;
    private const decimal MinCategorySpendToFlag = 20m;
    private const decimal MinSpikePercent = 15m;

    public async ValueTask<AiInsightsResult> Handle(GetAiInsightsQuery query, CancellationToken cancellationToken)
    {
        var insights = new List<Insight>();
        var now = dateTimeProvider.UtcNow.Date;

        var currency = (await companyRepository.GetByIdAsync(query.CompanyId, cancellationToken))?.Currency ?? "USD";

        await AddForecastInsights(query.CompanyId, now, currency, insights, cancellationToken);
        await AddAtRiskInvoicesInsight(query.CompanyId, now, currency, insights, cancellationToken);
        await AddCategorySpikeInsight(query.CompanyId, now, insights, cancellationToken);

        return new AiInsightsResult(insights);
    }

    private async Task AddForecastInsights(Guid companyId, DateTime now, string currency, List<Insight> insights, CancellationToken cancellationToken)
    {
        var forecast = await forecaster.ForecastAsync(companyId, HistoryDays, HistoryDays, cancellationToken);
        var current = forecast.CurrentBalance;

        // A squeeze: the projected balance goes below zero at some point in the next month.
        if (forecast.LowestBalance is < 0 && forecast.LowestBalanceDateUtc is { } lowDate)
        {
            insights.Add(new Insight(
                $"Cash could run short around {lowDate:MMM d}.",
                $"Your balance is projected to dip to {currency} {forecast.LowestBalance.Value:N2}. Plan ahead or move a payment.",
                InsightTone.Warning));
        }

        var upcomingDraw = forecast.Events.FirstOrDefault(e =>
            e.Kind == ForecastEventKind.OwnerDraw && e.DateUtc <= now.AddDays(UpcomingDrawWindowDays));
        if (upcomingDraw is not null)
        {
            insights.Add(new Insight(
                $"{upcomingDraw.Label} ({currency} {-upcomingDraw.Amount:N2}) is planned for {upcomingDraw.DateUtc:MMM d}.",
                "A planned personal withdrawal, already included in your forecast.",
                InsightTone.Caution));
        }

        var projected = forecast.Points.LastOrDefault()?.Forecast;
        if (current == 0 || projected is null) return;

        var percent = Math.Round((projected.Value - current) / Math.Abs(current) * 100, 1);
        if (percent == 0) return;

        var direction = percent > 0 ? "increase" : "decrease";
        insights.Add(new Insight(
            $"Cash flow is forecasted to {direction} by {Math.Abs(percent)}% next month.",
            "Based on your last 30 days, planned withdrawals and invoices due.",
            percent > 0 ? InsightTone.Positive : InsightTone.Warning));
    }

    private async Task AddAtRiskInvoicesInsight(Guid companyId, DateTime now, string currency, List<Insight> insights, CancellationToken cancellationToken)
    {
        var atRisk = await invoiceRepository.GetAtRiskSummaryAsync(companyId, now.AddDays(AtRiskWindowDays), cancellationToken);
        if (atRisk.Count == 0) return;

        var noun = atRisk.Count == 1 ? "customer is" : "customers are";
        insights.Add(new Insight(
            $"{atRisk.Count} {noun} at risk of paying late.",
            $"Total outstanding: {currency} {atRisk.TotalAmount:N2}",
            InsightTone.Warning));
    }

    private async Task AddCategorySpikeInsight(Guid companyId, DateTime now, List<Insight> insights, CancellationToken cancellationToken)
    {
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var thisMonthTx = await transactionRepository.GetInDateRangeAsync(companyId, thisMonthStart, thisMonthStart.AddMonths(1), cancellationToken);
        var lastMonthTx = await transactionRepository.GetInDateRangeAsync(companyId, lastMonthStart, thisMonthStart, cancellationToken);

        var thisMonthByCategory = ExpenseTotalsByCategory(thisMonthTx);
        var lastMonthByCategory = ExpenseTotalsByCategory(lastMonthTx);

        TransactionCategory? spikingCategory = null;
        var biggestPercent = 0m;

        foreach (var (category, thisTotal) in thisMonthByCategory)
        {
            if (thisTotal < MinCategorySpendToFlag) continue;
            if (!lastMonthByCategory.TryGetValue(category, out var lastTotal) || lastTotal == 0) continue;

            var percent = (thisTotal - lastTotal) / lastTotal * 100;
            if (percent >= MinSpikePercent && percent > biggestPercent)
            {
                biggestPercent = percent;
                spikingCategory = category;
            }
        }

        if (spikingCategory is null) return;

        insights.Add(new Insight(
            $"{CategoryLabel(spikingCategory.Value)} spending increased by {Math.Round(biggestPercent, 0)}% this month.",
            "This could reduce your projected cash runway.",
            InsightTone.Caution));
    }

    private static Dictionary<TransactionCategory, decimal> ExpenseTotalsByCategory(List<Transaction> transactions) =>
        transactions
            .Where(t => t.AmountInReportingCurrency < 0 && !t.Category.IsOwnerEquity())
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => -g.Sum(t => t.AmountInReportingCurrency));

    private static string CategoryLabel(TransactionCategory category) => category switch
    {
        TransactionCategory.OperatingExpense => "Operating expense",
        TransactionCategory.RentAndLease => "Rent & lease",
        _ => category.ToString(),
    };
}
