namespace FlowIQ.Contracts.CashFlowForecasting;

public record CashFlowPointResponse(DateTime DateUtc, decimal? Actual, decimal? Forecast);

public record CashFlowForecastResponse(IReadOnlyCollection<CashFlowPointResponse> Points);
