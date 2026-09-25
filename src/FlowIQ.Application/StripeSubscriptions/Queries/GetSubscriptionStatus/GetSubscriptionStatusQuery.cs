using FlowIQ.Domain.StripeSubscriptions;
using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionStatus;

public record GetSubscriptionStatusQuery(Guid CompanyId) : IQuery<SubscriptionStatusResult>;

public record SubscriptionStatusResult(
    bool HasSubscription,
    string? PlanKey,
    string? PlanDisplayName,
    decimal? MonthlyPriceUsd,
    SubscriptionStatus? Status,
    DateTime? CurrentPeriodEndUtc);
