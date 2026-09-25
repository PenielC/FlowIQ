namespace FlowIQ.Application.StripeSubscriptions;

public record SubscriptionPlan(string Key, string DisplayName, decimal MonthlyPriceUsd, string StripeLookupKey, IReadOnlyList<string> Features);

/// <summary>
/// FlowIQ's fixed pricing tiers. Not a database-managed catalog — two plans is simple enough
/// to hardcode, and each maps to a Stripe Price found-or-created by lookup key at checkout time
/// (see IStripeGateway), so nothing needs to be pre-configured in the Stripe dashboard by hand.
/// </summary>
public static class SubscriptionPlanCatalog
{
    public static readonly IReadOnlyList<SubscriptionPlan> Plans =
    [
        new("starter", "Starter", 29m, "flowiq_starter_monthly",
            ["Up to 500 transactions/mo", "Cash flow forecasting", "AI categorisation"]),
        new("growth", "Growth", 79m, "flowiq_growth_monthly",
            ["Unlimited transactions", "Everything in Starter", "Team members & roles", "Priority support"]),
    ];

    public static SubscriptionPlan? Find(string key) => Plans.FirstOrDefault(p => p.Key == key);

    public static SubscriptionPlan? FindByLookupKey(string lookupKey) => Plans.FirstOrDefault(p => p.StripeLookupKey == lookupKey);
}
