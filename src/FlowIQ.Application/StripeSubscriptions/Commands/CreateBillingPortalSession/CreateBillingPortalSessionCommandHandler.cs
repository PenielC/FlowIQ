using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Commands.CreateBillingPortalSession;

public class CreateBillingPortalSessionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IStripeGateway stripeGateway,
    IAppUrlProvider appUrlProvider) : ICommandHandler<CreateBillingPortalSessionCommand, PortalSessionResult>
{
    public async ValueTask<PortalSessionResult> Handle(CreateBillingPortalSessionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByCompanyIdAsync(command.CompanyId, cancellationToken);
        if (subscription is null)
        {
            throw new DomainException("No subscription found for this company yet.");
        }

        var returnUrl = $"{appUrlProvider.WebBaseUrl}/subscriptions";
        var session = await stripeGateway.CreateBillingPortalSessionAsync(subscription.StripeCustomerId, returnUrl, cancellationToken);

        return new PortalSessionResult(session.PortalUrl);
    }
}
