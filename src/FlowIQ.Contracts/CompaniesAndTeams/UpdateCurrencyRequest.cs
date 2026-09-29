namespace FlowIQ.Contracts.CompaniesAndTeams;

/// <param name="ManualRates">Rates for currencies with no live rate (from the preview), keyed by currency code.</param>
public record UpdateCurrencyRequest(string Currency, Dictionary<string, decimal>? ManualRates = null);

public record CurrencyRateNeedResponse(string Currency, int TransactionCount, int InvoiceCount, decimal? IndicativeRate);

public record CurrencyChangePreviewResponse(
    string FromCurrency,
    string ToCurrency,
    int TransactionCount,
    int InvoiceCount,
    List<CurrencyRateNeedResponse> Currencies);
