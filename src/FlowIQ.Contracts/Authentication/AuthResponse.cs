namespace FlowIQ.Contracts.Authentication;

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    UserResponse User);

public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid CompanyId,
    string CompanyName,
    string CompanyCurrency,
    bool IsPlatformAdmin);
