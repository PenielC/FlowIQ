namespace FlowIQ.Contracts.BankTransactions;

public record ImportRowRequest(DateTime TransactionDateUtc, string Description, decimal Amount, string Category);

public record ConfirmTransactionImportRequest(IReadOnlyCollection<ImportRowRequest> Rows, string Currency, decimal ExchangeRate);

public record ImportTransactionsResponse(int ImportedCount);
