namespace FlowIQ.Contracts.BankTransactions;

public record CreateTransactionRequest(
    string Description,
    string Category,
    decimal Amount,
    DateTime TransactionDateUtc,
    string Status,
    string Currency,
    decimal? ExchangeRate);
