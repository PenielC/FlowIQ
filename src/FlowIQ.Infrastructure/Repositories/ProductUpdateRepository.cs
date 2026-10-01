using FlowIQ.Application.ProductUpdates;
using FlowIQ.Domain.ProductUpdates;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class ProductUpdateRepository(ApplicationDbContext dbContext)
    : EfRepository<ProductUpdate>(dbContext), IProductUpdateRepository
{
    public Task<List<ProductUpdate>> ListPublishedAsync(int take, CancellationToken cancellationToken = default) =>
        DbContext.ProductUpdates.AsNoTracking()
            .Where(u => u.PublishedAtUtc != null)
            .OrderByDescending(u => u.PublishedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<List<ProductUpdate>> ListAllAsync(CancellationToken cancellationToken = default) =>
        DbContext.ProductUpdates.AsNoTracking()
            .OrderByDescending(u => u.PublishedAtUtc ?? u.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<HashSet<Guid>> DismissedIdsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await DbContext.ProductUpdateDismissals.AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => d.ProductUpdateId)
            .ToListAsync(cancellationToken)).ToHashSet();

    public async Task AddDismissalAsync(ProductUpdateDismissal dismissal, CancellationToken cancellationToken = default) =>
        await DbContext.ProductUpdateDismissals.AddAsync(dismissal, cancellationToken);
}
