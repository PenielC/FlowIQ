using FlowIQ.Domain.Common;

namespace FlowIQ.Domain.Invoicing;

public enum InvoiceEmailKind
{
    Invoice = 0,
    Reminder = 1,
}

public enum InvoiceEmailStatus
{
    Sent = 0,
    Failed = 1,

    /// <summary>
    /// A reminder day that had already passed when reminders were turned on (or the invoice got an email
    /// address); only the latest due reminder is sent, so the customer doesn't get several at once.
    /// </summary>
    Skipped = 2,
}

/// <summary>
/// One email about an invoice: the invoice itself, or a reminder for one of the company's reminder days.
/// A reminder day is recorded at most once per invoice unless it failed (a failed one is tried again on
/// the next run), which is what makes the reminder run safe to repeat.
/// </summary>
public class InvoiceEmail : BaseAuditableEntity
{
    private InvoiceEmail() { }

    public InvoiceEmail(
        Guid companyId,
        Guid invoiceId,
        InvoiceEmailKind kind,
        int? reminderDay,
        string toEmail,
        InvoiceEmailStatus status,
        string? error,
        DateTime atUtc)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        InvoiceId = invoiceId;
        Kind = kind;
        ReminderDay = reminderDay;
        ToEmail = toEmail;
        Status = status;
        Error = error is { Length: > 500 } ? error[..500] : error;
        AtUtc = atUtc;
    }

    public Guid CompanyId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public InvoiceEmailKind Kind { get; private set; }

    /// <summary>For a reminder: which "days after the due date" setting it was for.</summary>
    public int? ReminderDay { get; private set; }

    public string ToEmail { get; private set; } = string.Empty;
    public InvoiceEmailStatus Status { get; private set; }
    public string? Error { get; private set; }
    public DateTime AtUtc { get; private set; }
}
