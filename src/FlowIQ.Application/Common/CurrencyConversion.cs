using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.Common;

/// <summary>
/// Shared rate-resolution logic for anything that records a money amount in its own currency and needs it
/// converted into a company's reporting currency at creation time. A missing rate is never guessed: the caller
/// gets a clear error asking for the rate (it used to fall back to 1:1 silently, which turned R89 into $89).
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
            logger.LogWarning("No exchange rate available for {From}->{To}; asking for a manual rate.", recordCurrency, reportingCurrency);
            throw new DomainException(
                $"No live exchange rate is available for {recordCurrency.ToUpperInvariant()} to {reportingCurrency.ToUpperInvariant()}. Please enter the exchange rate.");
        }

        return liveRate.Value;
    }
}
