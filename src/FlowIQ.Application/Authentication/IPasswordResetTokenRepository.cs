using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;

namespace FlowIQ.Application.Authentication;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
