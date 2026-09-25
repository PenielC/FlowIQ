namespace FlowIQ.Contracts.BankTransactions;

public record TransactionResponse(
    Guid Id,
    string Description,
    string Category,
    decimal Amount,
    DateTime TransactionDateUtc,
    string Status,
    string Currency,
    decimal AmountInReportingCurrency);
