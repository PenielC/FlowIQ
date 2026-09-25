using FlowIQ.Domain.BankTransactions;

namespace FlowIQ.Application.BankTransactions.TransactionImport;

public record TransactionImportRowPreview(
    int RowNumber,
    DateTime? TransactionDateUtc,
    string Description,
    decimal? Amount,
    TransactionCategory SuggestedCategory,
    bool IsDuplicate,
    string? ParseError);
