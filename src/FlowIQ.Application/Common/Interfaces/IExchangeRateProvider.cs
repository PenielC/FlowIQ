namespace FlowIQ.Application.Common.Interfaces;

/// <summary>A live rate plus where it came from (some sources require attribution wherever the rate is shown).</summary>
public record ExchangeRateQuote(decimal Rate, string Source);

/// <summary>
/// Resolves currency conversion rates. Implementations must never throw for ordinary failures
/// (network errors, timeouts, an unsupported pair) — they return null so callers decide what to do
/// (the app asks the user for a rate rather than guessing one).
/// </summary>
public interface IExchangeRateProvider
{
    Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    Task<ExchangeRateQuote?> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Daily rates for a date range (days with no published rate — weekends, holidays — are simply absent),
    /// or null when no source has history for this pair.
    /// </summary>
    Task<IReadOnlyDictionary<DateOnly, decimal>?> GetHistoricalRatesAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
