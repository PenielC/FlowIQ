using Mediator;

namespace FlowIQ.Application.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResult>;
