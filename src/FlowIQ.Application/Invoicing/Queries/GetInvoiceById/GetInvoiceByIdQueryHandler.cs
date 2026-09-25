using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository) : IQueryHandler<GetInvoiceByIdQuery, InvoiceResult>
{
    public async ValueTask<InvoiceResult> Handle(GetInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (invoice is null || invoice.CompanyId != query.CompanyId)
        {
            throw new DomainException("Invoice not found.");
        }

        return new InvoiceResult(
            invoice.Id, invoice.CustomerName, invoice.Amount, invoice.IssueDateUtc, invoice.DueDateUtc, invoice.Status,
            invoice.Currency, invoice.AmountInReportingCurrency,
            invoice.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(),
            invoice.Notes);
    }
}
