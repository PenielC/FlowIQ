using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Domain.StripeSubscriptions;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class SubscriptionRepository(ApplicationDbContext dbContext)
    : EfRepository<Subscription>(dbContext), ISubscriptionRepository
{
    public Task<Subscription?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        DbContext.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken);

    public Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default) =>
        DbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);

    public Task<bool> HasProcessedEventAsync(string stripeEventId, CancellationToken cancellationToken = default) =>
        DbContext.ProcessedWebhookEvents.AnyAsync(e => e.StripeEventId == stripeEventId, cancellationToken);

    public async Task MarkEventProcessedAsync(string stripeEventId, DateTime processedAtUtc, CancellationToken cancellationToken = default) =>
        await DbContext.ProcessedWebhookEvents.AddAsync(new ProcessedWebhookEvent(stripeEventId, processedAtUtc), cancellationToken);
}
