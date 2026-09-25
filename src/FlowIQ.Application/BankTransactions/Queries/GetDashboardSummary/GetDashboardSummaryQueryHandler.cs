using Mediator;

namespace FlowIQ.Application.BankTransactions.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler(ITransactionRepository transactionRepository)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResult>
{
    public async ValueTask<DashboardSummaryResult> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        DateTime rangeStart;
        DateTime rangeEndExclusive;
        DateTime previousStart;
        if (query.StartDateUtc is null || query.EndDateUtc is null)
        {
            // Default view: current calendar month vs the actual previous calendar month — not just an
            // equal-length window, since calendar months don't all have the same number of days.
            rangeStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            rangeEndExclusive = rangeStart.AddMonths(1);
            previousStart = rangeStart.AddMonths(-1);
        }
        else
        {
            // Query-string-bound DateTimes come in as Kind=Unspecified — Npgsql requires UTC-kind values
            // for a timestamptz column, so this must be stamped explicitly rather than just taking .Date.
            rangeStart = DateTime.SpecifyKind(query.StartDateUtc.Value.Date, DateTimeKind.Utc);
            rangeEndExclusive = DateTime.SpecifyKind(query.EndDateUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            previousStart = rangeStart - (rangeEndExclusive - rangeStart);
        }

        var balance = await transactionRepository.GetBalanceBeforeDateAsync(query.CompanyId, rangeEndExclusive, cancellationToken);
        var currentTransactions = await transactionRepository.GetInDateRangeAsync(query.CompanyId, rangeStart, rangeEndExclusive, cancellationToken);
        var previousTransactions = await transactionRepository.GetInDateRangeAsync(query.CompanyId, previousStart, rangeStart, cancellationToken);
        var recent = await transactionRepository.GetRecentByCompanyAsync(query.CompanyId, 4, cancellationToken);

        var current = Totals(currentTransactions);
        var previous = Totals(previousTransactions);

        return new DashboardSummaryResult(
            balance,
            current.Income,
            PercentChange(previous.Income, current.Income),
            current.Expenses,
            PercentChange(previous.Expenses, current.Expenses),
            recent.Select(t => new TransactionResult(t.Id, t.Description, t.Category, t.Amount, t.TransactionDateUtc, t.Status, t.Currency, t.AmountInReportingCurrency)).ToList());
    }

    private static TransactionMonthTotals Totals(IEnumerable<Domain.BankTransactions.Transaction> transactions)
    {
        var income = transactions.Where(t => t.AmountInReportingCurrency > 0).Sum(t => t.AmountInReportingCurrency);
        var expenses = -transactions.Where(t => t.AmountInReportingCurrency < 0).Sum(t => t.AmountInReportingCurrency);
        return new TransactionMonthTotals(income, expenses);
    }

    private static decimal? PercentChange(decimal previous, decimal current)
    {
        if (previous == 0) return null;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
