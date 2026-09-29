using System.Globalization;
using System.Text.Json;
using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Infrastructure.ExchangeRates;

/// <summary>
/// Frankfurter (free, no API key, ECB reference rates, with daily history). Covers about 30 currencies, and of
/// African ones only ZAR. Never throws — any failure (network, timeout, unexpected response shape, unsupported
/// pair) is logged and returns null.
/// </summary>
public class FrankfurterExchangeRateProvider(HttpClient httpClient, ILogger<FrankfurterExchangeRateProvider> logger)
    : IExchangeRateProvider
{
    public const string SourceName = "Frankfurter";

    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default) =>
        (await GetQuoteAsync(fromCurrency, toCurrency, cancellationToken))?.Rate;

    public async Task<ExchangeRateQuote?> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new ExchangeRateQuote(1m, SourceName);
        }

        using var doc = await GetJsonAsync(
            $"latest?amount=1&from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}",
            fromCurrency, toCurrency, cancellationToken);
        if (doc is null) return null;

        if (!doc.RootElement.TryGetProperty("rates", out var rates) ||
            !rates.TryGetProperty(toCurrency.ToUpperInvariant(), out var rateElement))
        {
            logger.LogWarning("Frankfurter response for {From}->{To} did not contain the expected rate.", fromCurrency, toCurrency);
            return null;
        }

        return new ExchangeRateQuote(rateElement.GetDecimal(), SourceName);
    }

    public async Task<IReadOnlyDictionary<DateOnly, decimal>?> GetHistoricalRatesAsync(
        string fromCurrency, string toCurrency, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        // Start a week early so a record dated on a weekend or holiday still has an earlier published rate.
        var start = startDate.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var doc = await GetJsonAsync(
            $"{start}..{end}?base={Uri.EscapeDataString(fromCurrency)}&symbols={Uri.EscapeDataString(toCurrency)}",
            fromCurrency, toCurrency, cancellationToken);
        if (doc is null || !doc.RootElement.TryGetProperty("rates", out var days)) return null;

        var result = new Dictionary<DateOnly, decimal>();
        var key = toCurrency.ToUpperInvariant();
        foreach (var day in days.EnumerateObject())
        {
            if (DateOnly.TryParseExact(day.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                day.Value.TryGetProperty(key, out var rate))
            {
                result[date] = rate.GetDecimal();
            }
        }

        return result.Count > 0 ? result : null;
    }

    private async Task<JsonDocument?> GetJsonAsync(string path, string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 404 is how Frankfurter says "unsupported currency" — expected for most African currencies.
                logger.LogInformation("Frankfurter returned {StatusCode} for {From}->{To}.", response.StatusCode, fromCurrency, toCurrency);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Failed to fetch exchange rate {From}->{To} from Frankfurter.", fromCurrency, toCurrency);
            return null;
        }
    }
}
