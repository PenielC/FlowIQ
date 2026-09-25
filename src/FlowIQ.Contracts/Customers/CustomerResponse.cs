namespace FlowIQ.Contracts.Customers;

public record CustomerResponse(Guid Id, string Name, string? Email, string? Phone, string? Notes, DateTime CreatedAtUtc);
