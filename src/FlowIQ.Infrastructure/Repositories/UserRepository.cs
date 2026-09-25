using FlowIQ.Application.Authentication;
using FlowIQ.Domain.Authentication;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : EfRepository<User>(dbContext), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbContext.Users.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbContext.Users.AnyAsync(u => u.Email == email.Trim().ToLower(), cancellationToken);

    public Task<List<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        DbContext.Users.Where(u => u.CompanyId == companyId).ToListAsync(cancellationToken);

    public Task<int> CountByCompanyAndRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default) =>
        DbContext.Users.CountAsync(u => u.CompanyId == companyId && u.Role == role, cancellationToken);
}
