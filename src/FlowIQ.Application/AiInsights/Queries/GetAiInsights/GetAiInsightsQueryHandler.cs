using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
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
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetAiInsightsQuery, AiInsightsResult>
{
    private const int HistoryDays = 30;
    private const int AtRiskWindowDays = 7;
    private const decimal MinCategorySpendToFlag = 20m;
    private const decimal MinSpikePercent = 15m;

    public async ValueTask<AiInsightsResult> Handle(GetAiInsightsQuery query, CancellationToken cancellationToken)
    {
        var insights = new List<Insight>();
        var now = dateTimeProvider.UtcNow.Date;

        await AddForecastInsight(query.CompanyId, now, insights, cancellationToken);
        await AddAtRiskInvoicesInsight(query.CompanyId, now, insights, cancellationToken);
        await AddCategorySpikeInsight(query.CompanyId, now, insights, cancellationToken);

        return new AiInsightsResult(insights);
    }

    private async Task AddForecastInsight(Guid companyId, DateTime now, List<Insight> insights, CancellationToken cancellationToken)
    {
        var currentBalance = await transactionRepository.GetBalanceAsync(companyId, cancellationToken);
        var baseline = await transactionRepository.GetBalanceBeforeDateAsync(companyId, now.AddDays(-HistoryDays), cancellationToken);
        var netChangeOverHistory = currentBalance - baseline;

        if (currentBalance == 0) return;

        // Projecting the same trend forward for another HistoryDays gives the same net change again.
        var percent = Math.Round(netChangeOverHistory / Math.Abs(currentBalance) * 100, 1);
        if (percent == 0) return;

        var direction = percent > 0 ? "increase" : "decrease";
        insights.Add(new Insight(
            $"Cash flow is forecasted to {direction} by {Math.Abs(percent)}% next month.",
            "Based on your transaction history over the last 30 days.",
            percent > 0 ? InsightTone.Positive : InsightTone.Warning));
    }

    private async Task AddAtRiskInvoicesInsight(Guid companyId, DateTime now, List<Insight> insights, CancellationToken cancellationToken)
    {
        var atRisk = await invoiceRepository.GetAtRiskSummaryAsync(companyId, now.AddDays(AtRiskWindowDays), cancellationToken);
        if (atRisk.Count == 0) return;

        var noun = atRisk.Count == 1 ? "customer is" : "customers are";
        insights.Add(new Insight(
            $"{atRisk.Count} {noun} at risk of paying late.",
            $"Total outstanding: ${atRisk.TotalAmount:N2}",
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
            .Where(t => t.AmountInReportingCurrency < 0)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => -g.Sum(t => t.AmountInReportingCurrency));

    private static string CategoryLabel(TransactionCategory category) => category switch
    {
        TransactionCategory.OperatingExpense => "Operating expense",
        TransactionCategory.RentAndLease => "Rent & lease",
        _ => category.ToString(),
    };
}
