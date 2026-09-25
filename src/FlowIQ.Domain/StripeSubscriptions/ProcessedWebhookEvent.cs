using FlowIQ.Domain.Common;

namespace FlowIQ.Domain.StripeSubscriptions;

/// <summary>Records a processed Stripe event id so retried/duplicate webhook deliveries are safely ignored.</summary>
public class ProcessedWebhookEvent : BaseEntity, IAggregateRoot
{
    private ProcessedWebhookEvent() { }

    public ProcessedWebhookEvent(string stripeEventId, DateTime processedAtUtc)
    {
        Id = Guid.NewGuid();
        StripeEventId = stripeEventId;
        ProcessedAtUtc = processedAtUtc;
    }

    public string StripeEventId { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; private set; }
}
