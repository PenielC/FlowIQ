using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.StripeSubscriptions;

namespace FlowIQ.Application.StripeSubscriptions;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);

    Task<bool> HasProcessedEventAsync(string stripeEventId, CancellationToken cancellationToken = default);

    Task MarkEventProcessedAsync(string stripeEventId, DateTime processedAtUtc, CancellationToken cancellationToken = default);
}
