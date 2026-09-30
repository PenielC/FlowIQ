using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.CashFlowForecasting;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Common;

/// <summary>One currency a company has records in, and the rate that would convert it to the new reporting currency.</summary>
/// <param name="IndicativeRate">Today's rate, for display; null when no source has one (the user must supply it).</param>
public record CurrencyRateNeed(string Currency, int TransactionCount, int InvoiceCount, decimal? IndicativeRate);

public record CurrencyChangePreview(
    string FromCurrency,
    string ToCurrency,
    int TransactionCount,
    int InvoiceCount,
    IReadOnlyList<CurrencyRateNeed> Currencies);

public record RestatementSummary(int TransactionsRestated, int InvoicesRestated);

/// <summary>
/// Re-expresses a company's records in a new reporting currency. Every record is converted from its own original
/// amount and currency (never from the old reporting-currency figure), at the rate on its own date where history
/// exists (transaction date / invoice issue date), otherwise today's rate, otherwise a rate the user supplied.
/// Nothing is changed unless a rate is available for every currency involved.
/// </summary>
public class ReportingCurrencyConverter(
    ITransactionRepository transactionRepository,
    IInvoiceRepository invoiceRepository,
    IPlannedOwnerDrawRepository plannedOwnerDrawRepository,
    IExchangeRateProvider exchangeRateProvider)
{
    public async Task<CurrencyChangePreview> PreviewAsync(
        Guid companyId, string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListByCompanyAsync(companyId, cancellationToken);
        var invoices = await invoiceRepository.ListByCompanyAsync(companyId, cancellationToken);
        var draws = await plannedOwnerDrawRepository.ListByCompanyAsync(companyId, cancellationToken);
        var to = toCurrency.ToUpperInvariant();

        var needs = new List<CurrencyRateNeed>();
        var currencies = transactions.Select(t => t.Currency).Concat(invoices.Select(i => i.Currency)).Concat(draws.Select(d => d.Currency));
        foreach (var currency in currencies.Distinct().Order())
        {
            if (currency == to) continue;
            var rate = await exchangeRateProvider.GetRateAsync(currency, to, cancellationToken);
            needs.Add(new CurrencyRateNeed(
                currency,
                transactions.Count(t => t.Currency == currency),
                invoices.Count(i => i.Currency == currency),
                rate));
        }

        return new CurrencyChangePreview(fromCurrency.ToUpperInvariant(), to, transactions.Count, invoices.Count, needs);
    }

    /// <summary>Restates all of the company's records. Throws (changing nothing) if any currency has no rate.</summary>
    public async Task<RestatementSummary> RestateCompanyAsync(
        Guid companyId, string toCurrency, IReadOnlyDictionary<string, decimal>? manualRates, CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListByCompanyAsync(companyId, cancellationToken);
        var invoices = await invoiceRepository.ListByCompanyAsync(companyId, cancellationToken);
        var draws = await plannedOwnerDrawRepository.ListByCompanyAsync(companyId, cancellationToken);
        return await RestateAsync(transactions, invoices, toCurrency, manualRates, cancellationToken, draws);
    }

    /// <summary>Restates the given records. Rates are all resolved first; nothing changes if any is missing.</summary>
    public async Task<RestatementSummary> RestateAsync(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyCollection<Invoice> invoices,
        string toCurrency,
        IReadOnlyDictionary<string, decimal>? manualRates,
        CancellationToken cancellationToken,
        IReadOnlyCollection<PlannedOwnerDraw>? plannedDraws = null)
    {
        var to = toCurrency.ToUpperInvariant();
        var draws = plannedDraws ?? [];
        // Planned draws are future amounts, so they are converted at today's rate.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var manual = (manualRates ?? new Dictionary<string, decimal>())
            .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value);

        // (currency, date) of every record, so each currency's history is fetched once for its whole date span.
        var dated = transactions.Select(t => (t.Currency, Date: DateOnly.FromDateTime(t.TransactionDateUtc)))
            .Concat(invoices.Select(i => (i.Currency, Date: DateOnly.FromDateTime(i.IssueDateUtc))))
            .Concat(draws.Select(d => (d.Currency, Date: today)))
            .ToList();

        var rateFor = new Dictionary<string, Func<DateOnly, decimal>>();
        var missing = new List<string>();
        foreach (var group in dated.GroupBy(d => d.Currency))
        {
            var currency = group.Key;
            if (currency == to)
            {
                rateFor[currency] = _ => 1m;
            }
            else if (manual.TryGetValue(currency, out var manualRate) && manualRate > 0)
            {
                rateFor[currency] = _ => manualRate;
            }
            else
            {
                var history = await exchangeRateProvider.GetHistoricalRatesAsync(
                    currency, to, group.Min(d => d.Date), group.Max(d => d.Date), cancellationToken);
                if (history is { Count: > 0 })
                {
                    var days = history.OrderBy(kv => kv.Key).ToList();
                    rateFor[currency] = date => RateOn(days, date);
                    continue;
                }

                var latest = await exchangeRateProvider.GetRateAsync(currency, to, cancellationToken);
                if (latest is null)
                {
                    missing.Add(currency);
                    continue;
                }

                rateFor[currency] = _ => latest.Value;
            }
        }

        if (missing.Count > 0)
        {
            throw new DomainException(
                $"No exchange rate is available for {string.Join(", ", missing)} to {to}. Please enter the rate to use.");
        }

        foreach (var t in transactions)
        {
            t.RestateInReportingCurrency(rateFor[t.Currency](DateOnly.FromDateTime(t.TransactionDateUtc)));
        }

        foreach (var i in invoices)
        {
            i.RestateInReportingCurrency(rateFor[i.Currency](DateOnly.FromDateTime(i.IssueDateUtc)));
        }

        foreach (var d in draws)
        {
            d.RestateInReportingCurrency(rateFor[d.Currency](today));
        }

        return new RestatementSummary(transactions.Count, invoices.Count);
    }

    /// <summary>The rate published on the date, else the latest one before it, else the earliest one we have.</summary>
    private static decimal RateOn(List<KeyValuePair<DateOnly, decimal>> days, DateOnly date)
    {
        var rate = days[0].Value;
        foreach (var (day, value) in days)
        {
            if (day > date) break;
            rate = value;
        }

        return rate;
    }
}
