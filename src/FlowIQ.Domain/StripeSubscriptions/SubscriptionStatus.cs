namespace FlowIQ.Domain.StripeSubscriptions;

public enum SubscriptionStatus
{
    Incomplete = 0,
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Unpaid = 5,
}
