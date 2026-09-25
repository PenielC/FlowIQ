namespace FlowIQ.Contracts.Customers;

public record CreateCustomerRequest(string Name, string? Email, string? Phone, string? Notes);
