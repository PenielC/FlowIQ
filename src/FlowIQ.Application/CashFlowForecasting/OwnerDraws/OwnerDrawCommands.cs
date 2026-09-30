using FlowIQ.Domain.CashFlowForecasting;
using Mediator;

namespace FlowIQ.Application.CashFlowForecasting.OwnerDraws;

public record GetOwnerDrawsQuery(Guid CompanyId) : IQuery<OwnerDrawsResult>;

/// <param name="ExchangeRate">Only needed when <paramref name="Currency"/> differs from the reporting currency and no live rate exists.</param>
public record CreateOwnerDrawCommand(
    Guid CompanyId,
    string Name,
    decimal Amount,
    string Currency,
    decimal? ExchangeRate,
    DrawFrequency Frequency,
    DateTime NextDateUtc,
    IReadOnlyList<int>? Months) : ICommand<OwnerDrawResult>;

public record UpdateOwnerDrawCommand(
    Guid CompanyId,
    Guid DrawId,
    string Name,
    decimal Amount,
    string Currency,
    decimal? ExchangeRate,
    DrawFrequency Frequency,
    DateTime NextDateUtc,
    IReadOnlyList<int>? Months) : ICommand<OwnerDrawResult>;

public record DeleteOwnerDrawCommand(Guid CompanyId, Guid DrawId) : ICommand;

/// <summary>The owner answered the forecast setup (with draws, or "I take nothing out"), so stop asking.</summary>
public record CompleteForecastSetupCommand(Guid CompanyId) : ICommand;
