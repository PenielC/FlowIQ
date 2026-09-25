using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.CompaniesAndTeams;

public class Invitation : BaseEntity, IAggregateRoot
{
    private Invitation() { }

    public Invitation(Guid companyId, string email, UserRole role, string token, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        Token = token;
        Status = InvitationStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid CompanyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }

    public bool IsValid => Status == InvitationStatus.Pending && ExpiresAtUtc > DateTime.UtcNow;

    public void Accept()
    {
        if (!IsValid)
        {
            throw new DomainException("This invitation is no longer valid.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new DomainException("Only a pending invitation can be revoked.");
        }

        Status = InvitationStatus.Revoked;
    }
}
