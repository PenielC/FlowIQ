using FlowIQ.Domain.BankTransactions;

namespace FlowIQ.Application.BankTransactions;

public record TransactionResult(
    Guid Id,
    string Description,
    TransactionCategory Category,
    decimal Amount,
    DateTime TransactionDateUtc,
    TransactionStatus Status,
    string Currency,
    decimal AmountInReportingCurrency);
