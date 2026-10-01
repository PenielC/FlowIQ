using System.Security.Cryptography;
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
    /// <see cref="Amount"/> converted into the company's reporting currency. Locked at creation; restated (from
    /// <see cref="Amount"/>, at the rate on the issue date) only when the company changes its reporting currency.
    /// </summary>
    public decimal AmountInReportingCurrency { get; private set; }

    /// <summary>The rate used to compute <see cref="AmountInReportingCurrency"/>.</summary>
    public decimal ExchangeRateToReportingCurrency { get; private set; }

    /// <summary>Re-expresses this invoice in a (new) reporting currency, always from its original amount.</summary>
    public void RestateInReportingCurrency(decimal exchangeRate)
    {
        if (exchangeRate <= 0)
        {
            throw new DomainException("Exchange rate must be greater than zero.");
        }

        ExchangeRateToReportingCurrency = exchangeRate;
        AmountInReportingCurrency = Math.Round(Amount * exchangeRate, 2);
    }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    public string? Notes { get; private set; }

    /// <summary>Where the invoice and its reminders are emailed. Optional: without it, nothing is sent.</summary>
    public string? CustomerEmail { get; private set; }

    /// <summary>Set on an invoice that shouldn't be chased (an agreed delay, a sensitive customer).</summary>
    public bool RemindersPaused { get; private set; }

    /// <summary>
    /// The secret in the customer's "View invoice" link. Created the first time the invoice is emailed, so
    /// invoices that were never sent have no public link at all.
    /// </summary>
    public string? PublicToken { get; private set; }

    public bool IsUnpaid => Status is InvoiceStatus.Sent or InvoiceStatus.Overdue;

    public void SetCustomerEmail(string? email)
    {
        var trimmed = email?.Trim();
        CustomerEmail = string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }

    public void SetRemindersPaused(bool paused) => RemindersPaused = paused;

    public string EnsurePublicToken()
    {
        PublicToken ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return PublicToken;
    }

    /// <summary>A sent invoice whose due date has passed becomes Overdue. Returns whether it changed.</summary>
    public bool MarkOverdueIfPastDue(DateTime todayUtc)
    {
        if (Status != InvoiceStatus.Sent || DueDateUtc.Date >= todayUtc.Date)
        {
            return false;
        }

        Status = InvoiceStatus.Overdue;
        return true;
    }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new DomainException("Invoice is already paid.");
        }

        Status = InvoiceStatus.Paid;
    }
}
