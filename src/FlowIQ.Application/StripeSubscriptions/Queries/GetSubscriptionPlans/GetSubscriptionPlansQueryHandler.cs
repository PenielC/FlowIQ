using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansQueryHandler : IQueryHandler<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlan>>
{
    public ValueTask<IReadOnlyList<SubscriptionPlan>> Handle(GetSubscriptionPlansQuery query, CancellationToken cancellationToken) =>
        ValueTask.FromResult(SubscriptionPlanCatalog.Plans);
}
