using Mediator;

namespace FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;

public record GetCashFlowForecastQuery(Guid CompanyId, int HistoryDays = 30, int ForecastDays = 30)
    : IQuery<CashFlowForecastResult>;

public record CashFlowPoint(DateTime DateUtc, decimal? Actual, decimal? Forecast);

public record CashFlowForecastResult(IReadOnlyCollection<CashFlowPoint> Points);
