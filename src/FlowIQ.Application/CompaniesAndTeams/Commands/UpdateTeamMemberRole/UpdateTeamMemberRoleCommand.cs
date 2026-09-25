using FlowIQ.Domain.Authentication;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateTeamMemberRole;

public record UpdateTeamMemberRoleCommand(Guid CompanyId, Guid RequestedByUserId, Guid TargetUserId, UserRole NewRole)
    : ICommand<TeamMemberResult>;
