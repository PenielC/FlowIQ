namespace FlowIQ.Application.Customers;

public record CustomerResult(Guid Id, string Name, string? Email, string? Phone, string? Notes, DateTime CreatedAtUtc);
