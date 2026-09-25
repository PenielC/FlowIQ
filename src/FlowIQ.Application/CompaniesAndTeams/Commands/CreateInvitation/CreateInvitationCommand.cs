using FlowIQ.Domain.Authentication;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.CreateInvitation;

public record CreateInvitationCommand(Guid CompanyId, string Email, UserRole Role) : ICommand<InvitationResult>;
