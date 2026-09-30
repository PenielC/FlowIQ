namespace FlowIQ.Contracts.CashFlowForecasting;

public record CashFlowPointResponse(DateTime DateUtc, decimal? Actual, decimal? Forecast);

/// <param name="Kind">"OwnerDraw" or "InvoiceDue".</param>
/// <param name="Amount">In the reporting currency; negative is money going out.</param>
public record ForecastEventResponse(DateTime DateUtc, string Label, decimal Amount, string Kind);

public record CashFlowForecastResponse(
    IReadOnlyCollection<CashFlowPointResponse> Points,
    IReadOnlyCollection<ForecastEventResponse> Events,
    decimal CurrentBalance,
    decimal? LowestBalance,
    DateTime? LowestBalanceDateUtc);
