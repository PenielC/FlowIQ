using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Invoicing.Emails;

public record ReminderRunSummary(int MarkedOverdue, int Sent, int Failed, int Skipped);

/// <summary>
/// The reminder run. First every sent invoice past its due date becomes Overdue (for all companies, so
/// statuses are right whether or not reminders are on). Then, for each company with reminders on, each
/// unpaid invoice with a customer email and reminders not paused gets the reminder for the latest of the
/// company's reminder days it has reached and not had yet. Earlier days it has already passed are recorded
/// as skipped, so turning reminders on doesn't send a customer three emails at once.
///
/// Safe to run any number of times a day: a reminder day is recorded once per invoice (a database unique
/// index backs this up), and a failed send is simply tried again on a later run.
/// </summary>
public class InvoiceReminderRunner(
    IInvoiceReminderStore store,
    IInvoiceEmailRepository invoiceEmails,
    InvoiceMailer mailer,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork)
{
    public async Task<ReminderRunSummary> RunAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        var today = clock.UtcNow.Date;
        var marked = await store.MarkOverdueAsync(today, companyId, cancellationToken);
        int sent = 0, failed = 0, skipped = 0;

        foreach (var company in await store.CompaniesWithRemindersOnAsync(companyId, cancellationToken))
        {
            var invoices = await store.RemindableInvoicesAsync(company.Id, today, cancellationToken);
            if (invoices.Count == 0) continue;

            var handled = await invoiceEmails.GetHandledReminderDaysAsync(invoices.Select(i => i.Id).ToList(), cancellationToken);
            foreach (var invoice in invoices)
            {
                var daysOverdue = (today - invoice.DueDateUtc.Date).Days;
                var due = company.ReminderDays.Where(d => d <= daysOverdue && !handled.Contains((invoice.Id, d))).Order().ToList();
                if (due.Count == 0) continue;

                var latest = due[^1];
                foreach (var day in due[..^1])
                {
                    await invoiceEmails.AddAsync(
                        new InvoiceEmail(company.Id, invoice.Id, InvoiceEmailKind.Reminder, day, invoice.CustomerEmail!, InvoiceEmailStatus.Skipped, null, clock.UtcNow),
                        cancellationToken);
                    skipped++;
                }

                var record = await mailer.SendAsync(invoice, company, InvoiceEmailKind.Reminder, latest, null, null, cancellationToken);
                if (record.Status == InvoiceEmailStatus.Sent) sent++;
                else failed++;
            }

            // Saved per company, so one company's trouble doesn't lose another's records.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ReminderRunSummary(marked, sent, failed, skipped);
    }
}
