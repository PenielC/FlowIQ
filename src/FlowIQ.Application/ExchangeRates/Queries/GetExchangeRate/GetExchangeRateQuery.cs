using Mediator;

namespace FlowIQ.Application.ExchangeRates.Queries.GetExchangeRate;

public record GetExchangeRateQuery(string FromCurrency, string ToCurrency) : IQuery<ExchangeRateResult>;
