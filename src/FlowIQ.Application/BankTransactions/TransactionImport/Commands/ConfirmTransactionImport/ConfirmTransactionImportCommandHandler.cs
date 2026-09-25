using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using Mediator;

namespace FlowIQ.Application.BankTransactions.TransactionImport.Commands.ConfirmTransactionImport;

public class ConfirmTransactionImportCommandHandler(
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmTransactionImportCommand, ImportTransactionsResult>
{
    public async ValueTask<ImportTransactionsResult> Handle(ConfirmTransactionImportCommand command, CancellationToken cancellationToken)
    {
        foreach (var row in command.Rows)
        {
            var transaction = new Transaction(
                command.CompanyId,
                row.Description,
                row.Category,
                row.Amount,
                row.TransactionDateUtc,
                TransactionStatus.Completed,
                command.Currency,
                row.Amount * command.ExchangeRate,
                command.ExchangeRate);

            await transactionRepository.AddAsync(transaction, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportTransactionsResult(command.Rows.Count);
    }
}
