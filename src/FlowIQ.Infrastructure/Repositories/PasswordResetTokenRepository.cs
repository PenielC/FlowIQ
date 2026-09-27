using FlowIQ.Application.Authentication;
using FlowIQ.Domain.Authentication;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class PasswordResetTokenRepository(ApplicationDbContext dbContext)
    : EfRepository<PasswordResetToken>(dbContext), IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        DbContext.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
}
