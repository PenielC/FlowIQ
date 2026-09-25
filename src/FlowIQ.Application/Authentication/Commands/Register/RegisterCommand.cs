using Mediator;

namespace FlowIQ.Application.Authentication.Commands.Register;

public record RegisterCommand(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand<AuthResult>;
