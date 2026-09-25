namespace FlowIQ.Contracts.BankTransactions;

public record DashboardSummaryResponse(
    decimal CashBalance,
    decimal MonthlyRevenue,
    decimal? RevenueChangePercent,
    decimal MonthlyExpenses,
    decimal? ExpensesChangePercent,
    IReadOnlyCollection<TransactionResponse> RecentTransactions);
