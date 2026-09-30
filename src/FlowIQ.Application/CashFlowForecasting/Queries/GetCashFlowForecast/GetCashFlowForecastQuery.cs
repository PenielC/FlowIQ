using Mediator;

namespace FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;

public record GetCashFlowForecastQuery(Guid CompanyId, int HistoryDays = 30, int ForecastDays = 30)
    : IQuery<CashFlowForecast>;
