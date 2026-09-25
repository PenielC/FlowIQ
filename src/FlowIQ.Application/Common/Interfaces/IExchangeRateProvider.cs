namespace FlowIQ.Application.Common.Interfaces;

/// <summary>
/// Resolves a live currency conversion rate. Implementations must never throw for ordinary failures
/// (network errors, timeouts, an unsupported pair) — they return null so callers can fall back cleanly
/// instead of hard-failing whatever operation needed the rate.
/// </summary>
public interface IExchangeRateProvider
{
    Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}
