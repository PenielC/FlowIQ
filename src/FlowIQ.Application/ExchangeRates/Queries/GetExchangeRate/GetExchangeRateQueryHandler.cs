using FlowIQ.Application.Common.Interfaces;
using Mediator;

namespace FlowIQ.Application.ExchangeRates.Queries.GetExchangeRate;

public class GetExchangeRateQueryHandler(IExchangeRateProvider exchangeRateProvider)
    : IQueryHandler<GetExchangeRateQuery, ExchangeRateResult>
{
    public async ValueTask<ExchangeRateResult> Handle(GetExchangeRateQuery query, CancellationToken cancellationToken)
    {
        if (string.Equals(query.FromCurrency, query.ToCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new ExchangeRateResult(1m, true, null);
        }

        var quote = await exchangeRateProvider.GetQuoteAsync(query.FromCurrency, query.ToCurrency, cancellationToken);
        return new ExchangeRateResult(quote?.Rate, quote is not null, quote?.Source);
    }
}
