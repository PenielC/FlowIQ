using Mediator;

namespace FlowIQ.Application.Authentication.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommand<AuthResult>;
