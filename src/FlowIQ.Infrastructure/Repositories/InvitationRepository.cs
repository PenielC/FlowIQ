using FlowIQ.Application.CompaniesAndTeams;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class InvitationRepository(ApplicationDbContext dbContext)
    : EfRepository<Invitation>(dbContext), IInvitationRepository
{
    public Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        DbContext.Invitations.FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public Task<List<Invitation>> GetPendingByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        DbContext.Invitations
            .Where(i => i.CompanyId == companyId && i.Status == InvitationStatus.Pending)
            .ToListAsync(cancellationToken);
}
