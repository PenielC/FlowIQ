using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.Invoicing;

public class Invoice : BaseAuditableEntity, IAggregateRoot
{
    private Invoice() { }

    private readonly List<InvoiceLineItem> _lineItems = [];

    public Invoice(
        Guid companyId,
        string customerName,
        IReadOnlyCollection<(string Description, decimal Amount)> lineItems,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        InvoiceStatus status,
        string currency,
        decimal exchangeRateToReportingCurrency,
        string? notes)
    {
        if (lineItems is null || lineItems.Count == 0)
        {
            throw new DomainException("Invoice must have at least one line item.");
        }

        var amount = lineItems.Sum(li => li.Amount);
        if (amount <= 0)
        {
            throw new DomainException("Invoice amount must be greater than zero.");
        }

        if (dueDateUtc < issueDateUtc)
        {
            throw new DomainException("Invoice due date cannot be before the issue date.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter code.");
        }

        if (exchangeRateToReportingCurrency <= 0)
        {
            throw new DomainException("Exchange rate must be greater than zero.");
        }

        Id = Guid.NewGuid();
        CompanyId = companyId;
        CustomerName = customerName;
        Amount = amount;
        IssueDateUtc = issueDateUtc;
        DueDateUtc = dueDateUtc;
        Status = status;
        Currency = currency.ToUpperInvariant();
        AmountInReportingCurrency = amount * exchangeRateToReportingCurrency;
        ExchangeRateToReportingCurrency = exchangeRateToReportingCurrency;
        Notes = notes;

        foreach (var (description, lineAmount) in lineItems)
        {
            _lineItems.Add(new InvoiceLineItem(Id, description, lineAmount));
        }
    }

    public Guid CompanyId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>Sum of <see cref="LineItems"/>, in this invoice's own <see cref="Currency"/>.</summary>
    public decimal Amount { get; private set; }
    public DateTime IssueDateUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public InvoiceStatus Status { get; private set; }

    /// <summary>3-letter code this invoice was billed in — may differ from the company's reporting currency.</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>
    /// <see cref="Amount"/> converted into the company's reporting currency, using the rate locked in at creation
    /// time. Never recomputed retroactively if the company's reporting currency changes later.
    /// </summary>
    public decimal AmountInReportingCurrency { get; private set; }

    /// <summary>The rate used to compute <see cref="AmountInReportingCurrency"/> at creation time.</summary>
    public decimal ExchangeRateToReportingCurrency { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    public string? Notes { get; private set; }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new DomainException("Invoice is already paid.");
        }

        Status = InvoiceStatus.Paid;
    }
}
