using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoiceSummary;

public class GetInvoiceSummaryQueryHandler(IInvoiceRepository invoiceRepository)
    : IQueryHandler<GetInvoiceSummaryQuery, InvoiceSummaryResult>
{
    public async ValueTask<InvoiceSummaryResult> Handle(GetInvoiceSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await invoiceRepository.GetOutstandingSummaryAsync(query.CompanyId, cancellationToken);
        var upcoming = await invoiceRepository.GetUpcomingByCompanyAsync(query.CompanyId, 4, cancellationToken);

        return new InvoiceSummaryResult(
            summary.TotalOutstanding,
            summary.CustomerCount,
            upcoming.Select(i => InvoiceResult.From(i)).ToList());
    }
}
