using FlowIQ.Application.Authentication;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetTeamMembers;

public class GetTeamMembersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetTeamMembersQuery, IReadOnlyCollection<TeamMemberResult>>
{
    public async ValueTask<IReadOnlyCollection<TeamMemberResult>> Handle(GetTeamMembersQuery query, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetByCompanyIdAsync(query.CompanyId, cancellationToken);

        return users
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new TeamMemberResult(u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.CreatedAtUtc))
            .ToList();
    }
}
