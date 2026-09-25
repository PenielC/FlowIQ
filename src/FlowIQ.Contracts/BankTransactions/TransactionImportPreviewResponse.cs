namespace FlowIQ.Contracts.BankTransactions;

public record TransactionImportRowResponse(
    int RowNumber,
    DateTime? TransactionDateUtc,
    string Description,
    decimal? Amount,
    string SuggestedCategory,
    bool IsDuplicate,
    string? ParseError);

public record TransactionImportPreviewResponse(
    IReadOnlyCollection<TransactionImportRowResponse> Rows,
    decimal ExchangeRate,
    int ImportableCount);
