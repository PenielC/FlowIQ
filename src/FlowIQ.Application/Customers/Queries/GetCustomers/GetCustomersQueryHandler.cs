using Mediator;

namespace FlowIQ.Application.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler(ICustomerRepository customerRepository) : IQueryHandler<GetCustomersQuery, PagedCustomersResult>
{
    public async ValueTask<PagedCustomersResult> Handle(GetCustomersQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await customerRepository.GetPagedByCompanyAsync(
            query.CompanyId, query.PageNumber, query.PageSize, cancellationToken);

        var results = items
            .Select(c => new CustomerResult(c.Id, c.Name, c.Email, c.Phone, c.Notes, c.CreatedAtUtc))
            .ToList();

        return new PagedCustomersResult(results, query.PageNumber, query.PageSize, totalCount);
    }
}
