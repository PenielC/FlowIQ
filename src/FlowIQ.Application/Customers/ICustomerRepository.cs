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

    /// <summary>A customer of the company with exactly this name (ignoring case), if any.</summary>
    Task<Customer?> FindByNameAsync(Guid companyId, string name, CancellationToken cancellationToken = default);
}
