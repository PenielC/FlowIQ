using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoices;

public class GetInvoicesQueryHandler(IInvoiceRepository invoiceRepository) : IQueryHandler<GetInvoicesQuery, PagedInvoicesResult>
{
    public async ValueTask<PagedInvoicesResult> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await invoiceRepository.GetPagedByCompanyAsync(
            query.CompanyId, query.PageNumber, query.PageSize, cancellationToken);

        var results = items
            .Select(i => InvoiceResult.From(i))
            .ToList();

        return new PagedInvoicesResult(results, query.PageNumber, query.PageSize, totalCount);
    }
}
