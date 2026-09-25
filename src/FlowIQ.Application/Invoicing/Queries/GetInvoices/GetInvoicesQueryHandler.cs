using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoices;

public class GetInvoicesQueryHandler(IInvoiceRepository invoiceRepository) : IQueryHandler<GetInvoicesQuery, PagedInvoicesResult>
{
    public async ValueTask<PagedInvoicesResult> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await invoiceRepository.GetPagedByCompanyAsync(
            query.CompanyId, query.PageNumber, query.PageSize, cancellationToken);

        var results = items
            .Select(i => new InvoiceResult(
                i.Id, i.CustomerName, i.Amount, i.IssueDateUtc, i.DueDateUtc, i.Status, i.Currency, i.AmountInReportingCurrency,
                i.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(), i.Notes))
            .ToList();

        return new PagedInvoicesResult(results, query.PageNumber, query.PageSize, totalCount);
    }
}
