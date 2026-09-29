namespace FlowIQ.Contracts.Admin;

public record RepairCurrencyDataRequest(bool DryRun = true, bool BackfillPaidInvoiceIncome = false);

public record CompanyRepairRowResponse(
    Guid CompanyId,
    string CompanyName,
    string Currency,
    int TransactionsRestated,
    int InvoicesRestated,
    int IncomeRecorded,
    string? Problem);

public record RepairCurrencyDataResponse(
    bool DryRun,
    List<CompanyRepairRowResponse> Companies,
    int TransactionsRestated,
    int InvoicesRestated,
    int IncomeRecorded);
