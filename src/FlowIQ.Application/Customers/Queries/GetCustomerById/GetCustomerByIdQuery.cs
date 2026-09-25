using Mediator;

namespace FlowIQ.Application.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid CompanyId, Guid CustomerId) : IQuery<CustomerResult>;
