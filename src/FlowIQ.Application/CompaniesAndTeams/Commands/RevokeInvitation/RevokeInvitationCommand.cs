using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.RevokeInvitation;

public record RevokeInvitationCommand(Guid CompanyId, Guid InvitationId) : ICommand;
