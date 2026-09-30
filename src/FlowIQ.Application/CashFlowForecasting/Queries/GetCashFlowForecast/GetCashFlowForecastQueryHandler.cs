using Mediator;

namespace FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;

/// <summary>See <see cref="CashFlowForecaster"/> for how the forecast is built.</summary>
public class GetCashFlowForecastQueryHandler(CashFlowForecaster forecaster)
    : IQueryHandler<GetCashFlowForecastQuery, CashFlowForecast>
{
    public async ValueTask<CashFlowForecast> Handle(GetCashFlowForecastQuery query, CancellationToken cancellationToken) =>
        await forecaster.ForecastAsync(query.CompanyId, query.HistoryDays, query.ForecastDays, cancellationToken);
}
