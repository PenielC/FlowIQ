using FlowIQ.Domain.Authentication;

namespace FlowIQ.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    TimeSpan RefreshTokenLifetime { get; }
}
