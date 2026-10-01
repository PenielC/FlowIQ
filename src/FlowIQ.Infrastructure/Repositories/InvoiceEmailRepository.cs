using FlowIQ.Application.Invoicing.Emails;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class InvoiceEmailRepository(ApplicationDbContext dbContext)
    : EfRepository<InvoiceEmail>(dbContext), IInvoiceEmailRepository
{
    public Task<List<InvoiceEmail>> ListByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        DbContext.InvoiceEmails.AsNoTracking()
            .Where(e => e.InvoiceId == invoiceId)
            .OrderByDescending(e => e.AtUtc)
            .ToListAsync(cancellationToken);

    public async Task<HashSet<(Guid InvoiceId, int Day)>> GetHandledReminderDaysAsync(
        IReadOnlyCollection<Guid> invoiceIds, CancellationToken cancellationToken = default)
    {
        var rows = await DbContext.InvoiceEmails.AsNoTracking()
            .Where(e => invoiceIds.Contains(e.InvoiceId) && e.Kind == InvoiceEmailKind.Reminder
                && e.Status != InvoiceEmailStatus.Failed && e.ReminderDay != null)
            .Select(e => new { e.InvoiceId, Day = e.ReminderDay!.Value })
            .ToListAsync(cancellationToken);
        return rows.Select(r => (r.InvoiceId, r.Day)).ToHashSet();
    }
}

public class InvoiceReminderStore(ApplicationDbContext dbContext) : IInvoiceReminderStore
{
    public Task<int> MarkOverdueAsync(DateTime todayUtc, Guid? companyId, CancellationToken cancellationToken = default) =>
        dbContext.Invoices
            .Where(i => i.Status == InvoiceStatus.Sent && i.DueDateUtc < todayUtc.Date && (companyId == null || i.CompanyId == companyId))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InvoiceStatus.Overdue), cancellationToken);

    public Task<List<Company>> CompaniesWithRemindersOnAsync(Guid? companyId, CancellationToken cancellationToken = default) =>
        dbContext.Companies
            .Where(c => c.ReminderEnabled && c.IsActive && (companyId == null || c.Id == companyId))
            .ToListAsync(cancellationToken);

    public Task<List<Invoice>> RemindableInvoicesAsync(Guid companyId, DateTime todayUtc, CancellationToken cancellationToken = default) =>
        dbContext.Invoices
            .Where(i => i.CompanyId == companyId
                && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                && i.DueDateUtc < todayUtc.Date
                && !i.RemindersPaused
                && i.CustomerEmail != null)
            .OrderBy(i => i.DueDateUtc)
            .ToListAsync(cancellationToken);
}
