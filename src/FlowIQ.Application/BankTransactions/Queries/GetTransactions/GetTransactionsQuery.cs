using Mediator;

namespace FlowIQ.Application.BankTransactions.Queries.GetTransactions;

public record GetTransactionsQuery(Guid CompanyId, int PageNumber = 1, int PageSize = 20) : IQuery<PagedTransactionsResult>;

public record PagedTransactionsResult(IReadOnlyCollection<TransactionResult> Items, int PageNumber, int PageSize, int TotalCount);
