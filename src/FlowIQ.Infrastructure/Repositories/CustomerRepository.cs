using FlowIQ.Application.Customers;
using FlowIQ.Domain.Customers;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class CustomerRepository(ApplicationDbContext dbContext)
    : EfRepository<Customer>(dbContext), ICustomerRepository
{
    public async Task<(List<Customer> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Customers.Where(c => c.CompanyId == companyId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
