using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.RemoveTeamMember;

public record RemoveTeamMemberCommand(Guid CompanyId, Guid RequestedByUserId, Guid TargetUserId) : ICommand;
