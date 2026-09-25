using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.Common;

/// <summary>
/// Shared rate-resolution logic for anything that records a money amount in its own currency and needs it
/// converted into a company's reporting currency at creation time. Never lets a missing/failed exchange rate
/// block the caller from saving — falls back to a locked 1:1 rate and logs a warning instead.
/// </summary>
public static class CurrencyConversion
{
    public static async Task<decimal> ResolveRateAsync(
        IExchangeRateProvider exchangeRateProvider,
        ILogger logger,
        string recordCurrency,
        string reportingCurrency,
        decimal? manualRate,
        CancellationToken cancellationToken)
    {
        if (string.Equals(recordCurrency, reportingCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        if (manualRate.HasValue)
        {
            return manualRate.Value;
        }

        var liveRate = await exchangeRateProvider.GetRateAsync(recordCurrency, reportingCurrency, cancellationToken);
        if (liveRate is null)
        {
            logger.LogWarning(
                "Exchange rate unavailable for {From}->{To}; falling back to a 1:1 rate. The converted amount will be inaccurate until corrected.",
                recordCurrency, reportingCurrency);
            return 1m;
        }

        return liveRate.Value;
    }
}
