using FlowIQ.Domain.Authentication;

namespace FlowIQ.Application.Authentication;

public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid CompanyId,
    string CompanyName,
    string CompanyCurrency);
