using Mediator;

namespace FlowIQ.Application.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(Guid CompanyId, string Name, string? Email, string? Phone, string? Notes) : ICommand<CustomerResult>;
