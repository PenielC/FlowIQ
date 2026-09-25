namespace FlowIQ.Contracts.StripeSubscriptions;

public record SubscriptionPlanResponse(string Key, string DisplayName, decimal MonthlyPriceUsd, int TrialDays, IReadOnlyList<string> Features);
