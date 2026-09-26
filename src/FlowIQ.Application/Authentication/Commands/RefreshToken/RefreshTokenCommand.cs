using Mediator;

namespace FlowIQ.Application.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string? Platform = null) : ICommand<AuthResult>;
