using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Common;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class EfRepository<T>(ApplicationDbContext dbContext) : IRepository<T> where T : BaseEntity
{
    protected ApplicationDbContext DbContext { get; } = dbContext;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbContext.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await DbContext.Set<T>().ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbContext.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbContext.Set<T>().Update(entity);

    public void Remove(T entity) => DbContext.Set<T>().Remove(entity);
}
