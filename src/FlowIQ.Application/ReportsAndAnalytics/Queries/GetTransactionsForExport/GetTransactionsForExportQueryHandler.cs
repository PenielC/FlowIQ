using FlowIQ.Application.BankTransactions;
using Mediator;

namespace FlowIQ.Application.ReportsAndAnalytics.Queries.GetTransactionsForExport;

public class GetTransactionsForExportQueryHandler(ITransactionRepository transactionRepository)
    : IQueryHandler<GetTransactionsForExportQuery, IReadOnlyCollection<TransactionResult>>
{
    public async ValueTask<IReadOnlyCollection<TransactionResult>> Handle(
        GetTransactionsForExportQuery query, CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.GetInDateRangeAsync(
            query.CompanyId, query.StartUtcInclusive, query.EndUtcExclusive, cancellationToken);

        return transactions
            .OrderBy(t => t.TransactionDateUtc)
            .Select(t => new TransactionResult(t.Id, t.Description, t.Category, t.Amount, t.TransactionDateUtc, t.Status, t.Currency, t.AmountInReportingCurrency))
            .ToList();
    }
}
