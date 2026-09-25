using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetTeamMembers;

public record GetTeamMembersQuery(Guid CompanyId) : IQuery<IReadOnlyCollection<TeamMemberResult>>;
