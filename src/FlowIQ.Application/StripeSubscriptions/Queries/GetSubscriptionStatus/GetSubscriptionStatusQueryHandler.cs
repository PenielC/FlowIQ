using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IQueryHandler<GetSubscriptionStatusQuery, SubscriptionStatusResult>
{
    public async ValueTask<SubscriptionStatusResult> Handle(GetSubscriptionStatusQuery query, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByCompanyIdAsync(query.CompanyId, cancellationToken);
        if (subscription is null)
        {
            return new SubscriptionStatusResult(false, null, null, null, null, null);
        }

        var plan = SubscriptionPlanCatalog.Find(subscription.PlanKey);

        return new SubscriptionStatusResult(
            true,
            subscription.PlanKey,
            plan?.DisplayName,
            plan?.MonthlyPriceUsd,
            subscription.Status,
            subscription.CurrentPeriodEndUtc);
    }
}
