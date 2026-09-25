using FlowIQ.Application.Authentication;
using FlowIQ.Domain.Authentication;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class RefreshTokenRepository(ApplicationDbContext dbContext) : EfRepository<RefreshToken>(dbContext), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        DbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
}
