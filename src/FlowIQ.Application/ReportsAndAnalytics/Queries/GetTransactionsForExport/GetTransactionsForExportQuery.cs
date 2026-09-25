using FlowIQ.Application.BankTransactions;
using Mediator;

namespace FlowIQ.Application.ReportsAndAnalytics.Queries.GetTransactionsForExport;

public record GetTransactionsForExportQuery(Guid CompanyId, DateTime StartUtcInclusive, DateTime EndUtcExclusive)
    : IQuery<IReadOnlyCollection<TransactionResult>>;
