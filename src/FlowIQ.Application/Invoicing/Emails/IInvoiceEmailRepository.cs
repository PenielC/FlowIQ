using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Invoicing.Emails;

public interface IInvoiceEmailRepository : IRepository<InvoiceEmail>
{
    /// <summary>Every email about the invoice, newest first.</summary>
    Task<List<InvoiceEmail>> ListByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>(invoice, reminder day) pairs already handled (sent or skipped) for these invoices.</summary>
    Task<HashSet<(Guid InvoiceId, int Day)>> GetHandledReminderDaysAsync(IReadOnlyCollection<Guid> invoiceIds, CancellationToken cancellationToken = default);
}

/// <summary>What the daily reminder run needs to read, across companies.</summary>
public interface IInvoiceReminderStore
{
    /// <summary>Sent invoices whose due date has passed become Overdue. Returns how many changed.</summary>
    Task<int> MarkOverdueAsync(DateTime todayUtc, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Active companies with reminders switched on (or just the one asked for, if it has them on).</summary>
    Task<List<Company>> CompaniesWithRemindersOnAsync(Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Tracked: unpaid, past due, not paused, with a customer email.</summary>
    Task<List<Invoice>> RemindableInvoicesAsync(Guid companyId, DateTime todayUtc, CancellationToken cancellationToken = default);
}
