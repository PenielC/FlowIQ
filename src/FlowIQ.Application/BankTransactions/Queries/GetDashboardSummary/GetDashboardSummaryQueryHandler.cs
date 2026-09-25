using Mediator;

namespace FlowIQ.Application.BankTransactions.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler(ITransactionRepository transactionRepository)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResult>
{
    public async ValueTask<DashboardSummaryResult> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var previousMonthDate = now.AddMonths(-1);

        var balance = await transactionRepository.GetBalanceAsync(query.CompanyId, cancellationToken);
        var currentMonth = await transactionRepository.GetTotalsForMonthAsync(query.CompanyId, now.Year, now.Month, cancellationToken);
        var previousMonth = await transactionRepository.GetTotalsForMonthAsync(
            query.CompanyId, previousMonthDate.Year, previousMonthDate.Month, cancellationToken);
        var recent = await transactionRepository.GetRecentByCompanyAsync(query.CompanyId, 4, cancellationToken);

        return new DashboardSummaryResult(
            balance,
            currentMonth.Income,
            PercentChange(previousMonth.Income, currentMonth.Income),
            currentMonth.Expenses,
            PercentChange(previousMonth.Expenses, currentMonth.Expenses),
            recent.Select(t => new TransactionResult(t.Id, t.Description, t.Category, t.Amount, t.TransactionDateUtc, t.Status, t.Currency, t.AmountInReportingCurrency)).ToList());
    }

    private static decimal? PercentChange(decimal previous, decimal current)
    {
        if (previous == 0) return null;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
