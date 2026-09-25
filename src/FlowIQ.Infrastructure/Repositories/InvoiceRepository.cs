using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.Invoicing;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class InvoiceRepository(ApplicationDbContext dbContext)
    : EfRepository<Invoice>(dbContext), IInvoiceRepository
{
    public async Task<(List<Invoice> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Invoices.Where(i => i.CompanyId == companyId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.IssueDateUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<Invoice>> GetUpcomingByCompanyAsync(Guid companyId, int count, CancellationToken cancellationToken = default) =>
        DbContext.Invoices
            .Where(i => i.CompanyId == companyId && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue))
            .OrderBy(i => i.DueDateUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<OutstandingSummary> GetOutstandingSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var outstanding = await DbContext.Invoices
            .Where(i => i.CompanyId == companyId && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue))
            .Select(i => new { i.AmountInReportingCurrency, i.CustomerName })
            .ToListAsync(cancellationToken);

        var total = outstanding.Sum(i => i.AmountInReportingCurrency);
        var customerCount = outstanding.Select(i => i.CustomerName).Distinct().Count();

        return new OutstandingSummary(total, customerCount);
    }

    public async Task<List<InvoiceStatusTotal>> GetStatusBreakdownAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var invoices = await DbContext.Invoices
            .Where(i => i.CompanyId == companyId)
            .Select(i => new { i.Status, i.AmountInReportingCurrency })
            .ToListAsync(cancellationToken);

        return invoices
            .GroupBy(i => i.Status)
            .Select(g => new InvoiceStatusTotal(g.Key, g.Count(), g.Sum(i => i.AmountInReportingCurrency)))
            .OrderBy(t => t.Status)
            .ToList();
    }

    public async Task<AtRiskSummary> GetAtRiskSummaryAsync(Guid companyId, DateTime dueOnOrBeforeUtc, CancellationToken cancellationToken = default)
    {
        var atRisk = await DbContext.Invoices
            .Where(i => i.CompanyId == companyId
                && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                && i.DueDateUtc <= dueOnOrBeforeUtc)
            .Select(i => i.AmountInReportingCurrency)
            .ToListAsync(cancellationToken);

        return new AtRiskSummary(atRisk.Count, atRisk.Sum());
    }
}
