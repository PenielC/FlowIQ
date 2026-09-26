using Mediator;

namespace FlowIQ.Application.Authentication.Commands.Login;

public record LoginCommand(string Email, string Password, string? Platform = null) : ICommand<AuthResult>;
