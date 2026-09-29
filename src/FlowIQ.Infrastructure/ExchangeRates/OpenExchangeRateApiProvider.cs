using System.Text.Json;
using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Infrastructure.ExchangeRates;

/// <summary>
/// ExchangeRate-API's open endpoint (open.er-api.com, free, no key): 160+ currencies including ZWG, KES, NGN, GHS,
/// ZMW, BWP. Updated daily, latest rates only (no history on the free tier). Its terms require an attribution link
/// wherever these rates are shown, which is why quotes carry their <see cref="ExchangeRateQuote.Source"/>.
/// Results are cached for an hour per base currency.
/// </summary>
public class OpenExchangeRateApiProvider(HttpClient httpClient, ILogger<OpenExchangeRateApiProvider> logger)
    : IExchangeRateProvider
{
    public const string SourceName = "ExchangeRate-API";

    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(1);
    private static readonly Dictionary<string, (DateTime FetchedAtUtc, Dictionary<string, decimal> Rates)> Cache = new();
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default) =>
        (await GetQuoteAsync(fromCurrency, toCurrency, cancellationToken))?.Rate;

    public async Task<ExchangeRateQuote?> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        var rates = await GetRatesForBaseAsync(fromCurrency.ToUpperInvariant(), cancellationToken);
        return rates is not null && rates.TryGetValue(toCurrency.ToUpperInvariant(), out var rate) && rate > 0
            ? new ExchangeRateQuote(rate, SourceName)
            : null;
    }

    public Task<IReadOnlyDictionary<DateOnly, decimal>?> GetHistoricalRatesAsync(
        string fromCurrency, string toCurrency, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<DateOnly, decimal>?>(null);

    private async Task<Dictionary<string, decimal>?> GetRatesForBaseAsync(string baseCurrency, CancellationToken cancellationToken)
    {
        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (Cache.TryGetValue(baseCurrency, out var cached) && DateTime.UtcNow - cached.FetchedAtUtc < CacheFor)
            {
                return cached.Rates;
            }
        }
        finally
        {
            CacheLock.Release();
        }

        try
        {
            using var response = await httpClient.GetAsync($"latest/{Uri.EscapeDataString(baseCurrency)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ExchangeRate-API returned {StatusCode} for base {Base}.", response.StatusCode, baseCurrency);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("result", out var result) || result.GetString() != "success" ||
                !doc.RootElement.TryGetProperty("rates", out var ratesElement))
            {
                logger.LogWarning("ExchangeRate-API had no rates for base {Base}.", baseCurrency);
                return null;
            }

            var rates = ratesElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetDecimal());
            await CacheLock.WaitAsync(cancellationToken);
            try
            {
                Cache[baseCurrency] = (DateTime.UtcNow, rates);
            }
            finally
            {
                CacheLock.Release();
            }

            return rates;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Failed to fetch exchange rates for base {Base} from ExchangeRate-API.", baseCurrency);
            return null;
        }
    }
}
