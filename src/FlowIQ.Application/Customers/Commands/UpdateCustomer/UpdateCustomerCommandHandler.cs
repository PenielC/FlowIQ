using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCustomerCommand, CustomerResult>
{
    public async ValueTask<CustomerResult> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null || customer.CompanyId != command.CompanyId)
        {
            throw new DomainException("Customer not found.");
        }

        customer.UpdateDetails(command.Name, command.Email, command.Phone, command.Notes);
        customerRepository.Update(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerResult(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Notes, customer.CreatedAtUtc);
    }
}
