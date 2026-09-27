using Mediator;

namespace FlowIQ.Application.Authentication.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
