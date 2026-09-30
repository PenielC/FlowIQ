namespace FlowIQ.Contracts.CashFlowForecasting;

/// <param name="Frequency">"Once", "Weekly", "Monthly" or "SelectedMonths".</param>
/// <param name="NextDateUtc">The next payment date; its day of the month (or week) is repeated.</param>
/// <param name="Months">1-12, only for "SelectedMonths" (e.g. the months school terms start).</param>
/// <param name="ExchangeRate">Only needed when the currency differs from the reporting currency and no live rate exists.</param>
public record SaveOwnerDrawRequest(
    string Name,
    decimal Amount,
    string Currency,
    decimal? ExchangeRate,
    string Frequency,
    DateTime NextDateUtc,
    IReadOnlyList<int>? Months);

public record OwnerDrawResponse(
    Guid Id,
    string Name,
    decimal Amount,
    string Currency,
    decimal AmountInReportingCurrency,
    string Frequency,
    DateTime NextDateUtc,
    IReadOnlyList<int> Months);

/// <param name="SetupCompleted">Whether the owner has answered the forecast setup question yet.</param>
public record OwnerDrawsResponse(bool SetupCompleted, IReadOnlyList<OwnerDrawResponse> Draws);
