using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.StripeSubscriptions;
using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IStripeGateway stripeGateway,
    IUnitOfWork unitOfWork,
    IAppUrlProvider appUrlProvider) : ICommandHandler<CreateCheckoutSessionCommand, CheckoutSessionResult>
{
    public async ValueTask<CheckoutSessionResult> Handle(CreateCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        var plan = SubscriptionPlanCatalog.Find(command.PlanKey)
            ?? throw new DomainException($"Unknown plan '{command.PlanKey}'.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(command.CompanyId, cancellationToken);

        if (subscription is null)
        {
            var stripeCustomerId = await stripeGateway.GetOrCreateCustomerAsync(
                command.CompanyId, command.UserEmail, command.CompanyName, cancellationToken);
            subscription = new Subscription(command.CompanyId, plan.Key, stripeCustomerId);
            await subscriptionRepository.AddAsync(subscription, cancellationToken);
        }

        var successUrl = $"{appUrlProvider.WebBaseUrl}/subscriptions?checkout=success";
        var cancelUrl = $"{appUrlProvider.WebBaseUrl}/subscriptions?checkout=cancelled";

        var session = await stripeGateway.CreateCheckoutSessionAsync(
            subscription.StripeCustomerId, plan.StripeLookupKey, successUrl, cancelUrl, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CheckoutSessionResult(session.CheckoutUrl);
    }
}
