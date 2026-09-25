namespace FlowIQ.Application.StripeSubscriptions;

public record SubscriptionPlan(
    string Key, string DisplayName, decimal MonthlyPriceUsd, int TrialDays, string StripeLookupKey, IReadOnlyList<string> Features);

/// <summary>
/// FlowIQ's pricing — a single plan is simple enough to hardcode rather than database-manage.
/// It maps to a Stripe Price found-or-created by lookup key at checkout time (see IStripeGateway),
/// so nothing needs to be pre-configured in the Stripe dashboard by hand.
/// </summary>
public static class SubscriptionPlanCatalog
{
    public static readonly IReadOnlyList<SubscriptionPlan> Plans =
    [
        new("standard", "Standard", 10m, 30, "flowiq_standard_monthly",
            ["Unlimited transactions", "AI-powered forecasting & categorisation", "Multi-currency invoicing",
                "Team members & roles", "Reports & CSV import"]),
    ];

    public static SubscriptionPlan? Find(string key) => Plans.FirstOrDefault(p => p.Key == key);

    public static SubscriptionPlan? FindByLookupKey(string lookupKey) => Plans.FirstOrDefault(p => p.StripeLookupKey == lookupKey);
}
