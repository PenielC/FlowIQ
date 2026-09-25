namespace FlowIQ.Contracts.StripeSubscriptions;

public record SubscriptionStatusResponse(
    bool HasSubscription,
    string? PlanKey,
    string? PlanDisplayName,
    decimal? MonthlyPriceUsd,
    string? Status,
    DateTime? CurrentPeriodEndUtc);
