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
            upcoming.Select(i => new InvoiceResult(
                i.Id, i.CustomerName, i.Amount, i.IssueDateUtc, i.DueDateUtc, i.Status, i.Currency, i.AmountInReportingCurrency,
                i.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(), i.Notes)).ToList());
    }
}
