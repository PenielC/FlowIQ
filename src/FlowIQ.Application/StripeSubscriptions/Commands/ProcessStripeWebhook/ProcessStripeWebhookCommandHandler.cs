using FlowIQ.Application.Common.Interfaces;
using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Commands.ProcessStripeWebhook;

public class ProcessStripeWebhookCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IStripeGateway stripeGateway,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ProcessStripeWebhookCommand, WebhookProcessResult>
{
    public async ValueTask<WebhookProcessResult> Handle(ProcessStripeWebhookCommand command, CancellationToken cancellationToken)
    {
        var stripeEvent = stripeGateway.ConstructEvent(command.RequestBody, command.SignatureHeader);

        // Stripe retries webhook deliveries (and can occasionally send true duplicates) — this
        // guards against double-applying the same event rather than trusting delivery is exactly-once.
        if (await subscriptionRepository.HasProcessedEventAsync(stripeEvent.EventId, cancellationToken))
        {
            return new WebhookProcessResult(Processed: false);
        }

        if (stripeEvent.StripeCustomerId is not null && stripeEvent.StripeSubscriptionId is not null && stripeEvent.Status is not null)
        {
            var subscription = await subscriptionRepository.GetByStripeCustomerIdAsync(stripeEvent.StripeCustomerId, cancellationToken);
            if (subscription is not null)
            {
                var plan = stripeEvent.PlanLookupKey is not null
                    ? SubscriptionPlanCatalog.FindByLookupKey(stripeEvent.PlanLookupKey)
                    : null;

                subscription.ApplyStripeUpdate(
                    stripeEvent.StripeSubscriptionId,
                    plan?.Key ?? subscription.PlanKey,
                    stripeEvent.Status.Value,
                    stripeEvent.CurrentPeriodEndUtc);

                subscriptionRepository.Update(subscription);
            }
        }

        await subscriptionRepository.MarkEventProcessedAsync(stripeEvent.EventId, dateTimeProvider.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WebhookProcessResult(Processed: true);
    }
}
