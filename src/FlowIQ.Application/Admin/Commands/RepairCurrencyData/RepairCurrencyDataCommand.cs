using Mediator;

namespace FlowIQ.Application.Admin.Commands.RepairCurrencyData;

/// <summary>
/// One-off repair of data written before 2026-09-29: records saved at a silent 1:1 rate, records relabelled (not
/// converted) when a company changed its reporting currency, and, optionally, paid invoices that never recorded
/// income. <paramref name="DryRun"/> reports what would change and saves nothing.
/// </summary>
public record RepairCurrencyDataCommand(bool DryRun, bool BackfillPaidInvoiceIncome) : ICommand<RepairCurrencyDataResult>;

public record CompanyRepairRow(
    Guid CompanyId,
    string CompanyName,
    string Currency,
    int TransactionsRestated,
    int InvoicesRestated,
    int IncomeRecorded,
    string? Problem);

public record RepairCurrencyDataResult(
    bool DryRun,
    List<CompanyRepairRow> Companies,
    int TransactionsRestated,
    int InvoicesRestated,
    int IncomeRecorded);
