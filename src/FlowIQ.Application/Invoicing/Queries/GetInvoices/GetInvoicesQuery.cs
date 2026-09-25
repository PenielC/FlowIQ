using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoices;

public record GetInvoicesQuery(Guid CompanyId, int PageNumber = 1, int PageSize = 20) : IQuery<PagedInvoicesResult>;

public record PagedInvoicesResult(IReadOnlyCollection<InvoiceResult> Items, int PageNumber, int PageSize, int TotalCount);
