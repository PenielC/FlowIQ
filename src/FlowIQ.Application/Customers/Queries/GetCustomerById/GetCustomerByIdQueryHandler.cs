using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler(ICustomerRepository customerRepository) : IQueryHandler<GetCustomerByIdQuery, CustomerResult>
{
    public async ValueTask<CustomerResult> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (customer is null || customer.CompanyId != query.CompanyId)
        {
            throw new DomainException("Customer not found.");
        }

        return new CustomerResult(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Notes, customer.CreatedAtUtc);
    }
}
