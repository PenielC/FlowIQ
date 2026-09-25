using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery : IQuery<IReadOnlyList<SubscriptionPlan>>;
