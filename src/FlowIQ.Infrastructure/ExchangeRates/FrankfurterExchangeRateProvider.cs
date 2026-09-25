using System.Text.Json;
using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Infrastructure.ExchangeRates;

/// <summary>
/// Calls Frankfurter.app (free, no API key, ECB reference rates) for a live conversion rate. Never throws —
/// any failure (network, timeout, unexpected response shape, unsupported currency pair) is caught and logged,
/// returning null so callers can fall back to a manual rate or a safe default instead of hard-failing.
/// </summary>
public class FrankfurterExchangeRateProvider(HttpClient httpClient, ILogger<FrankfurterExchangeRateProvider> logger)
    : IExchangeRateProvider
{
    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        try
        {
            using var response = await httpClient.GetAsync(
                $"latest?amount=1&from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Frankfurter returned {StatusCode} for {From}->{To}.",
                    response.StatusCode, fromCurrency, toCurrency);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("rates", out var rates) ||
                !rates.TryGetProperty(toCurrency.ToUpperInvariant(), out var rateElement))
            {
                logger.LogWarning("Frankfurter response for {From}->{To} did not contain the expected rate.", fromCurrency, toCurrency);
                return null;
            }

            return rateElement.GetDecimal();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Failed to fetch exchange rate {From}->{To} from Frankfurter.", fromCurrency, toCurrency);
            return null;
        }
    }
}
