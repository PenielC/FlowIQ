using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using Mediator;

namespace FlowIQ.Application.ReportsAndAnalytics.Queries.GetReportsSummary;

public record GetReportsSummaryQuery(Guid CompanyId, int MonthsBack = 6) : IQuery<ReportsSummaryResult>;

public record MonthlyTrendPoint(int Year, int Month, decimal Revenue, decimal Expenses);

public record CategoryTotal(TransactionCategory Category, decimal Total);

public record ReportsSummaryResult(
    IReadOnlyCollection<MonthlyTrendPoint> MonthlyTrend,
    IReadOnlyCollection<CategoryTotal> CategoryBreakdown,
    IReadOnlyCollection<InvoiceStatusTotal> InvoiceStatusBreakdown);
