using FlowIQ.Domain.Common;

namespace FlowIQ.Domain.Authentication;

public class User : BaseAuditableEntity, IAggregateRoot
{
    private User() { }

    public User(Guid companyId, string email, string passwordHash, string firstName, string lastName, UserRole role)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    public Guid CompanyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    public void ChangeRole(UserRole newRole) => Role = newRole;

    public void SetPasswordHash(string newHash) => PasswordHash = newHash;
}
