using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.Invoicing;

public class InvoiceLineItem : BaseEntity
{
    private InvoiceLineItem() { }

    public InvoiceLineItem(Guid invoiceId, string description, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Line item description is required.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Line item amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        Description = description;
        Amount = amount;
    }

    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
}
