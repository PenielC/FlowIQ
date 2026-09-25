using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using Mediator;

namespace FlowIQ.Application.ReportsAndAnalytics.Queries.GetReportsSummary;

public class GetReportsSummaryQueryHandler(
    ITransactionRepository transactionRepository,
    IInvoiceRepository invoiceRepository,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetReportsSummaryQuery, ReportsSummaryResult>
{
    public async ValueTask<ReportsSummaryResult> Handle(GetReportsSummaryQuery query, CancellationToken cancellationToken)
    {
        var monthsBack = Math.Clamp(query.MonthsBack, 1, 24);
        var now = dateTimeProvider.UtcNow;

        var trend = new List<MonthlyTrendPoint>();
        for (var i = monthsBack - 1; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var totals = await transactionRepository.GetTotalsForMonthAsync(query.CompanyId, monthDate.Year, monthDate.Month, cancellationToken);
            trend.Add(new MonthlyTrendPoint(monthDate.Year, monthDate.Month, totals.Income, totals.Expenses));
        }

        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodStart = currentMonthStart.AddMonths(-(monthsBack - 1));
        var periodEnd = currentMonthStart.AddMonths(1);

        var transactions = await transactionRepository.GetInDateRangeAsync(query.CompanyId, periodStart, periodEnd, cancellationToken);
        var categoryBreakdown = transactions
            .Where(t => t.AmountInReportingCurrency < 0)
            .GroupBy(t => t.Category)
            .Select(g => new CategoryTotal(g.Key, -g.Sum(t => t.AmountInReportingCurrency)))
            .OrderByDescending(c => c.Total)
            .ToList();

        var invoiceStatusBreakdown = await invoiceRepository.GetStatusBreakdownAsync(query.CompanyId, cancellationToken);

        return new ReportsSummaryResult(trend, categoryBreakdown, invoiceStatusBreakdown);
    }
}
