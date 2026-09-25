using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Customers;

namespace FlowIQ.Application.Customers;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<(List<Customer> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
