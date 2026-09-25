using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.Customers;

public class Customer : BaseAuditableEntity, IAggregateRoot
{
    private Customer() { }

    public Customer(Guid companyId, string name, string? email, string? phone, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name;
        Email = email;
        Phone = phone;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateDetails(string name, string? email, string? phone, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        Name = name;
        Email = email;
        Phone = phone;
        Notes = notes;
    }
}
