using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

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

    public Guid CompanyId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TransactionCategory Category { get; private set; }

    /// <summary>Positive is income, negative is an expense. In this transaction's own <see cref="Currency"/>.</summary>
    public decimal Amount { get; private set; }
    public DateTime TransactionDateUtc { get; private set; }
    public TransactionStatus Status { get; private set; }

    /// <summary>3-letter code this transaction was recorded in — may differ from the company's reporting currency.</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>
    /// <see cref="Amount"/> converted into the company's reporting currency, using the rate locked in at creation
    /// time (<see cref="ExchangeRateToReportingCurrency"/>). Never recomputed retroactively if the company's
    /// reporting currency changes later — this is historical-cost accounting, not a live conversion.
    /// </summary>
    public decimal AmountInReportingCurrency { get; private set; }

    /// <summary>
    /// The rate used to compute <see cref="AmountInReportingCurrency"/> at creation time. A value of 1 means either
    /// the transaction's own currency matched the company's reporting currency at the time, or the exchange rate
    /// provider was unavailable and this transaction fell back to a 1:1 rate.
    /// </summary>
    public decimal ExchangeRateToReportingCurrency { get; private set; }
}
