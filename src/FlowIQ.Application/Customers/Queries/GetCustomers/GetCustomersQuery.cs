using Mediator;

namespace FlowIQ.Application.Customers.Queries.GetCustomers;

public record GetCustomersQuery(Guid CompanyId, int PageNumber = 1, int PageSize = 20) : IQuery<PagedCustomersResult>;

public record PagedCustomersResult(IReadOnlyCollection<CustomerResult> Items, int PageNumber, int PageSize, int TotalCount);
