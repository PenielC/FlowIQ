namespace FlowIQ.Contracts.ReportsAndAnalytics;

public record MonthlyTrendPointResponse(int Year, int Month, decimal Revenue, decimal Expenses);

public record CategoryTotalResponse(string Category, decimal Total);

public record InvoiceStatusTotalResponse(string Status, int Count, decimal TotalAmount);

public record ReportsSummaryResponse(
    IReadOnlyCollection<MonthlyTrendPointResponse> MonthlyTrend,
    IReadOnlyCollection<CategoryTotalResponse> CategoryBreakdown,
    IReadOnlyCollection<InvoiceStatusTotalResponse> InvoiceStatusBreakdown);
