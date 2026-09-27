using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.Authentication;

public class PasswordResetToken : BaseEntity, IAggregateRoot
{
    private PasswordResetToken() { }

    public PasswordResetToken(Guid userId, string token, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    public bool IsValid => UsedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public void MarkUsed()
    {
        if (!IsValid)
        {
            throw new DomainException("This reset link is invalid or has expired.");
        }

        UsedAtUtc = DateTime.UtcNow;
    }
}
