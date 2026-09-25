using FlowIQ.Domain.BankTransactions;
using Mediator;

namespace FlowIQ.Application.BankTransactions.TransactionImport.Commands.ConfirmTransactionImport;

public record ImportRowInput(DateTime TransactionDateUtc, string Description, decimal Amount, TransactionCategory Category);

public record ConfirmTransactionImportCommand(
    Guid CompanyId,
    IReadOnlyCollection<ImportRowInput> Rows,
    string Currency,
    decimal ExchangeRate) : ICommand<ImportTransactionsResult>;

public record ImportTransactionsResult(int ImportedCount);
