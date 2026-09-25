using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;

namespace FlowIQ.Application.CompaniesAndTeams;

public interface IInvitationRepository : IRepository<Invitation>
{
    Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<Invitation>> GetPendingByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
