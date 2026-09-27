using Mediator;

namespace FlowIQ.Application.Authentication.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand;
