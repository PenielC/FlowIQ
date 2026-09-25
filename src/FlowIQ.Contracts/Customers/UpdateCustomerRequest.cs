namespace FlowIQ.Contracts.Customers;

public record UpdateCustomerRequest(string Name, string? Email, string? Phone, string? Notes);
