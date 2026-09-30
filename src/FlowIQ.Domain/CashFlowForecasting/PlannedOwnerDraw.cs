using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.CashFlowForecasting;

public enum DrawFrequency
{
    Once = 0,
    Weekly = 1,
    Monthly = 2,

    /// <summary>On the same day of the chosen months each year, e.g. school fees at the start of each term.</summary>
    SelectedMonths = 3,
}

/// <summary>
/// A regular amount the owner takes out of the business for personal costs (school fees, home rent, household).
/// The forecast places each occurrence on its date, so a big term-start payment shows up as a dip on that day
/// instead of being averaged away. <see cref="NextDateUtc"/> anchors the schedule: its day of the month (or day
/// of the week, for weekly draws) is repeated.
/// </summary>
public class PlannedOwnerDraw : BaseAuditableEntity, IAggregateRoot
{
    private PlannedOwnerDraw() { }

    public PlannedOwnerDraw(
        Guid companyId,
        string name,
        decimal amount,
        string currency,
        decimal amountInReportingCurrency,
        decimal exchangeRateToReportingCurrency,
        DrawFrequency frequency,
        DateTime nextDateUtc,
        IReadOnlyCollection<int>? months)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Update(name, amount, currency, amountInReportingCurrency, exchangeRateToReportingCurrency, frequency, nextDateUtc, months);
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>Always positive, in <see cref="Currency"/>; it is a cash outflow.</summary>
    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "USD";
    public decimal AmountInReportingCurrency { get; private set; }
    public decimal ExchangeRateToReportingCurrency { get; private set; }
    public DrawFrequency Frequency { get; private set; }
    public DateTime NextDateUtc { get; private set; }

    /// <summary>Months (1-12) for <see cref="DrawFrequency.SelectedMonths"/>; empty otherwise.</summary>
    public List<int> Months { get; private set; } = [];

    public void Update(
        string name,
        decimal amount,
        string currency,
        decimal amountInReportingCurrency,
        decimal exchangeRateToReportingCurrency,
        DrawFrequency frequency,
        DateTime nextDateUtc,
        IReadOnlyCollection<int>? months)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Give this withdrawal a name, like \"School fees\".");
        }

        if (amount <= 0)
        {
            throw new DomainException("The amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter code.");
        }

        if (exchangeRateToReportingCurrency <= 0)
        {
            throw new DomainException("Exchange rate must be greater than zero.");
        }

        var chosenMonths = (months ?? []).Distinct().Order().ToList();
        if (frequency == DrawFrequency.SelectedMonths)
        {
            if (chosenMonths.Count == 0)
            {
                throw new DomainException("Choose at least one month.");
            }

            if (chosenMonths.Any(m => m is < 1 or > 12))
            {
                throw new DomainException("Months must be between 1 and 12.");
            }
        }
        else
        {
            chosenMonths = [];
        }

        Name = name.Trim();
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        AmountInReportingCurrency = amountInReportingCurrency;
        ExchangeRateToReportingCurrency = exchangeRateToReportingCurrency;
        Frequency = frequency;
        NextDateUtc = DateTime.SpecifyKind(nextDateUtc.Date, DateTimeKind.Utc);
        Months = chosenMonths;
    }

    /// <summary>Re-expresses the amount in a (new) reporting currency, from its original amount.</summary>
    public void RestateInReportingCurrency(decimal exchangeRate)
    {
        if (exchangeRate <= 0)
        {
            throw new DomainException("Exchange rate must be greater than zero.");
        }

        ExchangeRateToReportingCurrency = exchangeRate;
        AmountInReportingCurrency = Math.Round(Amount * exchangeRate, 2);
    }

    /// <summary>Every date this draw falls on between the two dates (inclusive), never before <see cref="NextDateUtc"/>.</summary>
    public IEnumerable<DateTime> OccurrencesBetween(DateTime fromUtc, DateTime toUtc)
    {
        var from = fromUtc.Date < NextDateUtc ? NextDateUtc : fromUtc.Date;
        var to = toUtc.Date;
        if (from > to)
        {
            yield break;
        }

        switch (Frequency)
        {
            case DrawFrequency.Once:
                if (NextDateUtc >= from && NextDateUtc <= to)
                {
                    yield return NextDateUtc;
                }

                break;

            case DrawFrequency.Weekly:
                var weeksToSkip = (int)Math.Ceiling((from - NextDateUtc).TotalDays / 7);
                for (var day = NextDateUtc.AddDays(weeksToSkip * 7); day <= to; day = day.AddDays(7))
                {
                    yield return day;
                }

                break;

            case DrawFrequency.Monthly:
            case DrawFrequency.SelectedMonths:
                for (var month = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc); month <= to; month = month.AddMonths(1))
                {
                    if (Frequency == DrawFrequency.SelectedMonths && !Months.Contains(month.Month))
                    {
                        continue;
                    }

                    // The anchor day, pulled back to the month's last day where it doesn't exist (31st in June).
                    var day = month.AddDays(Math.Min(NextDateUtc.Day, DateTime.DaysInMonth(month.Year, month.Month)) - 1);
                    if (day >= from && day <= to)
                    {
                        yield return day;
                    }
                }

                break;
        }
    }
}
