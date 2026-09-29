using FlowIQ.Application.Common.Interfaces;

namespace FlowIQ.Infrastructure.ExchangeRates;

/// <summary>
/// Frankfurter first (ECB reference rates, with history), then ExchangeRate-API for the currencies Frankfurter
/// doesn't cover (most African currencies). History only comes from Frankfurter.
/// </summary>
public class CompositeExchangeRateProvider(
    FrankfurterExchangeRateProvider frankfurter,
    OpenExchangeRateApiProvider openExchangeRateApi) : IExchangeRateProvider
{
    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default) =>
        (await GetQuoteAsync(fromCurrency, toCurrency, cancellationToken))?.Rate;

    public async Task<ExchangeRateQuote?> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default) =>
        await frankfurter.GetQuoteAsync(fromCurrency, toCurrency, cancellationToken)
        ?? await openExchangeRateApi.GetQuoteAsync(fromCurrency, toCurrency, cancellationToken);

    public async Task<IReadOnlyDictionary<DateOnly, decimal>?> GetHistoricalRatesAsync(
        string fromCurrency, string toCurrency, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        await frankfurter.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate, cancellationToken)
        ?? await openExchangeRateApi.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate, cancellationToken);
}
