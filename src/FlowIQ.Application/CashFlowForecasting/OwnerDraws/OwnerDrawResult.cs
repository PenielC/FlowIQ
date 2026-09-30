using FlowIQ.Domain.CashFlowForecasting;

namespace FlowIQ.Application.CashFlowForecasting.OwnerDraws;

public record OwnerDrawResult(
    Guid Id,
    string Name,
    decimal Amount,
    string Currency,
    decimal AmountInReportingCurrency,
    DrawFrequency Frequency,
    DateTime NextDateUtc,
    IReadOnlyList<int> Months)
{
    public static OwnerDrawResult From(PlannedOwnerDraw d) =>
        new(d.Id, d.Name, d.Amount, d.Currency, d.AmountInReportingCurrency, d.Frequency, d.NextDateUtc, d.Months);
}

public record OwnerDrawsResult(bool SetupCompleted, IReadOnlyList<OwnerDrawResult> Draws);
