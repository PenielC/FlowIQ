using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Customers.Commands.DeleteCustomer;

public class DeleteCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteCustomerCommand>
{
    public async ValueTask<Unit> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null || customer.CompanyId != command.CompanyId)
        {
            throw new DomainException("Customer not found.");
        }

        customerRepository.Remove(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
