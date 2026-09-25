using FlowIQ.Domain.BankTransactions;
using Mediator;

namespace FlowIQ.Application.BankTransactions.Commands.CreateTransaction;

public record CreateTransactionCommand(
    Guid CompanyId,
    string Description,
    TransactionCategory Category,
    decimal Amount,
    DateTime TransactionDateUtc,
    TransactionStatus Status,
    string Currency,
    decimal? ExchangeRate) : ICommand<TransactionResult>;
