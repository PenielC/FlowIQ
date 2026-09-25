using Mediator;

namespace FlowIQ.Application.BankTransactions.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery(Guid CompanyId) : IQuery<DashboardSummaryResult>;

public record DashboardSummaryResult(
    decimal CashBalance,
    decimal MonthlyRevenue,
    decimal? RevenueChangePercent,
    decimal MonthlyExpenses,
    decimal? ExpensesChangePercent,
    IReadOnlyCollection<TransactionResult> RecentTransactions);
