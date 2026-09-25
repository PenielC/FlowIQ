using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Customers;
using Mediator;

namespace FlowIQ.Application.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCustomerCommand, CustomerResult>
{
    public async ValueTask<CustomerResult> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = new Customer(command.CompanyId, command.Name, command.Email, command.Phone, command.Notes);
        await customerRepository.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerResult(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Notes, customer.CreatedAtUtc);
    }
}
