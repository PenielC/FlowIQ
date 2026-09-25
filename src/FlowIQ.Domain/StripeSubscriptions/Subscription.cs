using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.StripeSubscriptions;

public class Subscription : BaseAuditableEntity, IAggregateRoot
{
    private Subscription() { }

    public Subscription(Guid companyId, string planKey, string stripeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(planKey))
        {
            throw new DomainException("Plan key is required.");
        }

        if (string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            throw new DomainException("Stripe customer id is required.");
        }

        Id = Guid.NewGuid();
        CompanyId = companyId;
        PlanKey = planKey;
        StripeCustomerId = stripeCustomerId;
        Status = SubscriptionStatus.Incomplete;
    }

    public Guid CompanyId { get; private set; }
    public string PlanKey { get; private set; } = string.Empty;
    public string StripeCustomerId { get; private set; } = string.Empty;
    public string? StripeSubscriptionId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime? CurrentPeriodEndUtc { get; private set; }

    public void ApplyStripeUpdate(string stripeSubscriptionId, string planKey, SubscriptionStatus status, DateTime? currentPeriodEndUtc)
    {
        StripeSubscriptionId = stripeSubscriptionId;
        PlanKey = planKey;
        Status = status;
        CurrentPeriodEndUtc = currentPeriodEndUtc;
    }

    public void MarkCanceled()
    {
        Status = SubscriptionStatus.Canceled;
    }
}
