using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Invoicing.Commands.MarkInvoicePaid;

public class MarkInvoicePaidCommandHandler(
    IInvoiceRepository invoiceRepository,
    ITransactionRepository transactionRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<MarkInvoicePaidCommand, InvoiceResult>
{
    public async ValueTask<InvoiceResult> Handle(MarkInvoicePaidCommand command, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null || invoice.CompanyId != command.CompanyId)
        {
            throw new DomainException("Invoice not found.");
        }

        invoice.MarkAsPaid();
        invoiceRepository.Update(invoice);
        // The payment is income: without this, revenue, cash balance, forecast and reports never saw paid invoices.
        await transactionRepository.AddAsync(Transaction.ForPaidInvoice(invoice, dateTimeProvider.UtcNow), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InvoiceResult(
            invoice.Id, invoice.CustomerName, invoice.Amount, invoice.IssueDateUtc, invoice.DueDateUtc, invoice.Status,
            invoice.Currency, invoice.AmountInReportingCurrency,
            invoice.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(),
            invoice.Notes);
    }
}
