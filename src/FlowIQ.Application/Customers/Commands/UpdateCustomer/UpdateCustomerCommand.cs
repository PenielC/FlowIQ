using Mediator;

namespace FlowIQ.Application.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid CompanyId,
    Guid CustomerId,
    string Name,
    string? Email,
    string? Phone,
    string? Notes) : ICommand<CustomerResult>;
