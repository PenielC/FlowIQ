using Mediator;

namespace FlowIQ.Application.BankTransactions.Queries.GetTransactions;

public class GetTransactionsQueryHandler(ITransactionRepository transactionRepository)
    : IQueryHandler<GetTransactionsQuery, PagedTransactionsResult>
{
    public async ValueTask<PagedTransactionsResult> Handle(GetTransactionsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await transactionRepository.GetPagedByCompanyAsync(
            query.CompanyId, query.PageNumber, query.PageSize, cancellationToken);

        var results = items
            .Select(t => new TransactionResult(t.Id, t.Description, t.Category, t.Amount, t.TransactionDateUtc, t.Status, t.Currency, t.AmountInReportingCurrency))
            .ToList();

        return new PagedTransactionsResult(results, query.PageNumber, query.PageSize, totalCount);
    }
}
