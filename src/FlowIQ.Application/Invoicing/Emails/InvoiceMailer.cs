using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.Invoicing.Emails;

/// <summary>
/// Sends one invoice or reminder email and records the outcome. A failed send is recorded as Failed (and
/// logged) rather than thrown, so the caller decides what a failure means; the record is added to the
/// unit of work and saved by the caller.
/// </summary>
public class InvoiceMailer(
    IEmailSender emailSender,
    IInvoiceEmailRepository invoiceEmails,
    IAppUrlProvider appUrls,
    IDateTimeProvider clock,
    ILogger<InvoiceMailer> logger)
{
    public string ViewUrl(Invoice invoice) => $"{appUrls.WebBaseUrl.TrimEnd('/')}/i/{invoice.EnsurePublicToken()}";

    public async Task<InvoiceEmail> SendAsync(
        Invoice invoice,
        Company company,
        InvoiceEmailKind kind,
        int? reminderDay,
        string? message,
        string? replyTo,
        CancellationToken cancellationToken)
    {
        var to = invoice.CustomerEmail ?? throw new InvalidOperationException("The invoice has no customer email.");
        var now = clock.UtcNow;
        var url = ViewUrl(invoice);
        var composed = kind == InvoiceEmailKind.Invoice
            ? InvoiceEmailComposer.Invoice(invoice, company.Name, url, message)
            : InvoiceEmailComposer.Reminder(invoice, company.Name, url, Math.Max(1, (now.Date - invoice.DueDateUtc.Date).Days));

        InvoiceEmail record;
        try
        {
            await emailSender.SendAsync(
                new OutgoingEmail(to, composed.Subject, composed.Html, composed.Text, $"{company.Name} via FinFlow", replyTo),
                cancellationToken);
            record = new InvoiceEmail(company.Id, invoice.Id, kind, reminderDay, to, InvoiceEmailStatus.Sent, null, now);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Sending {Kind} email for invoice {InvoiceId} failed.", kind, invoice.Id);
            record = new InvoiceEmail(company.Id, invoice.Id, kind, reminderDay, to, InvoiceEmailStatus.Failed, ex.Message, now);
        }

        await invoiceEmails.AddAsync(record, cancellationToken);
        return record;
    }
}
