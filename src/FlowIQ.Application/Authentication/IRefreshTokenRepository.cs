using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;

namespace FlowIQ.Application.Authentication;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
