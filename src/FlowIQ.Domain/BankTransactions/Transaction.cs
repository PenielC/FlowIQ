using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Domain.BankTransactions;

public class Transaction : BaseAuditableEntity, IAggregateRoot
{
    private Transaction() { }

    public Transaction(
        Guid companyId,
        string description,
        TransactionCategory category,
        decimal amount,
        DateTime transactionDateUtc,
        TransactionStatus status,
        string currency,
        decimal amountInReportingCurrency,
        decimal exchangeRateToReportingCurrency)
    {
        if (amount == 0)
        {
            throw new DomainException("Transaction amount cannot be zero.");
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
        Description = description;
        Category = category;
        Amount = amount;
        TransactionDateUtc = transactionDateUtc;
        Status = status;
        Currency = currency.ToUpperInvariant();
        AmountInReportingCurrency = amountInReportingCurrency;
        ExchangeRateToReportingCurrency = exchangeRateToReportingCurrency;
    }

    /// <summary>
    /// The income recorded when an invoice is marked paid, so revenue, balance, forecast and reports all see it.
    /// The amount and rate come from the invoice, so paying it never creates a currency gain or loss.
    /// </summary>
    public static Transaction ForPaidInvoice(Invoice invoice, DateTime paidAtUtc)
    {
        var transaction = new Transaction(
            invoice.CompanyId,
            $"Invoice paid: {invoice.CustomerName}",
            TransactionCategory.Sales,
            invoice.Amount,
            paidAtUtc,
            TransactionStatus.Completed,
            invoice.Currency,
            invoice.AmountInReportingCurrency,
            invoice.ExchangeRateToReportingCurrency);
        transaction.InvoiceId = invoice.Id;
        return transaction;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>Set when this is the income recorded for a paid invoice; at most one transaction per invoice.</summary>
    public Guid? InvoiceId { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public TransactionCategory Category { get; private set; }

    /// <summary>Positive is income, negative is an expense. In this transaction's own <see cref="Currency"/>.</summary>
    public decimal Amount { get; private set; }
    public DateTime TransactionDateUtc { get; private set; }
    public TransactionStatus Status { get; private set; }

    /// <summary>3-letter code this transaction was recorded in — may differ from the company's reporting currency.</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>
    /// <see cref="Amount"/> converted into the company's reporting currency at the rate in
    /// <see cref="ExchangeRateToReportingCurrency"/>. Locked at creation; restated (from <see cref="Amount"/>, at the
    /// rate on the transaction's own date) only when the company changes its reporting currency.
    /// </summary>
    public decimal AmountInReportingCurrency { get; private set; }

    /// <summary>
    /// The rate used to compute <see cref="AmountInReportingCurrency"/>. 1 when the transaction's own currency is the
    /// reporting currency. (Before 2026-09-29 a missing live rate also silently became 1; that no longer happens.)
    /// </summary>
    public decimal ExchangeRateToReportingCurrency { get; private set; }

    /// <summary>Re-expresses this transaction in a (new) reporting currency, always from its original amount.</summary>
    public void RestateInReportingCurrency(decimal exchangeRate)
    {
        if (exchangeRate <= 0)
        {
            throw new DomainException("Exchange rate must be greater than zero.");
        }

        ExchangeRateToReportingCurrency = exchangeRate;
        AmountInReportingCurrency = Math.Round(Amount * exchangeRate, 2);
    }
}
