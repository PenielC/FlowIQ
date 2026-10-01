using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Invoicing;

public record OutstandingSummary(decimal TotalOutstanding, int CustomerCount);

public record InvoiceStatusTotal(InvoiceStatus Status, int Count, decimal TotalAmount);

public record AtRiskSummary(int Count, decimal TotalAmount);

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<(List<Invoice> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<Invoice>> GetUpcomingByCompanyAsync(
        Guid companyId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>Every invoice of the company, tracked (for restating in a new reporting currency).</summary>
    Task<List<Invoice>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<OutstandingSummary> GetOutstandingSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<List<InvoiceStatusTotal>> GetStatusBreakdownAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Unpaid invoices (Sent or Overdue) due on or before <paramref name="dueOnOrBeforeUtc"/> — i.e. already overdue or due soon.</summary>
    Task<AtRiskSummary> GetAtRiskSummaryAsync(Guid companyId, DateTime dueOnOrBeforeUtc, CancellationToken cancellationToken = default);

    /// <summary>The invoice behind a customer's "View invoice" link, or null.</summary>
    Task<Invoice?> GetByPublicTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Sent (not overdue, not draft) invoices due between the two dates inclusive: the income the forecast can count on.</summary>
    Task<List<Invoice>> GetPendingDueBetweenAsync(Guid companyId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
