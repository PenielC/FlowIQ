using FlowIQ.Application.Authentication;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.AcceptInvitation;

public record AcceptInvitationCommand(string Token, string FirstName, string LastName, string Password) : ICommand<AuthResult>;
