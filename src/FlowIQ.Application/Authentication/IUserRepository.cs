using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;

namespace FlowIQ.Application.Authentication;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<int> CountByCompanyAndRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default);
}
