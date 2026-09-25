using Mediator;

namespace FlowIQ.Application.Customers.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid CompanyId, Guid CustomerId) : ICommand;
