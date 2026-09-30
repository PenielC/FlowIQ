using FlowIQ.Application.CashFlowForecasting;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class PlannedOwnerDrawRepository(ApplicationDbContext dbContext)
    : EfRepository<PlannedOwnerDraw>(dbContext), IPlannedOwnerDrawRepository
{
    public Task<List<PlannedOwnerDraw>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        DbContext.PlannedOwnerDraws
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.NextDateUtc)
            .ToListAsync(cancellationToken);
}
