namespace FlowIQ.Contracts.Authentication;

public record ResetPasswordRequest(string Token, string NewPassword);
