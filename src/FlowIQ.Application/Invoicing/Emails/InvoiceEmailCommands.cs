using FlowIQ.Domain.Invoicing;
using Mediator;

namespace FlowIQ.Application.Invoicing.Emails;

public record InvoiceEmailResult(Guid Id, InvoiceEmailKind Kind, int? ReminderDay, string ToEmail, InvoiceEmailStatus Status, string? Error, DateTime AtUtc)
{
    public static InvoiceEmailResult From(InvoiceEmail e) => new(e.Id, e.Kind, e.ReminderDay, e.ToEmail, e.Status, e.Error, e.AtUtc);
}

/// <param name="ToEmail">Optional: saved as the invoice's customer email when given.</param>
/// <param name="Message">Optional: replaces the default opening line of the email.</param>
public record SendInvoiceEmailCommand(Guid CompanyId, Guid UserId, Guid InvoiceId, string? ToEmail, string? Message) : ICommand<InvoiceEmailResult>;

/// <summary>Sets where the invoice is emailed and whether it's left out of reminders.</summary>
public record UpdateInvoiceDeliveryCommand(Guid CompanyId, Guid InvoiceId, string? CustomerEmail, bool RemindersPaused) : ICommand<InvoiceResult>;

public record GetInvoiceEmailsQuery(Guid CompanyId, Guid InvoiceId) : IQuery<IReadOnlyList<InvoiceEmailResult>>;

public record ReminderSettingsResult(bool Enabled, IReadOnlyList<int> Days);

public record GetReminderSettingsQuery(Guid CompanyId) : IQuery<ReminderSettingsResult>;

public record UpdateReminderSettingsCommand(Guid CompanyId, bool Enabled, IReadOnlyList<int> Days) : ICommand<ReminderSettingsResult>;

/// <summary>Runs the reminder pass for one company now, instead of waiting for the next scheduled run.</summary>
public record RunInvoiceRemindersCommand(Guid CompanyId) : ICommand<ReminderRunSummary>;

public record PublicInvoiceResult(
    string BusinessName,
    string? LogoDataUrl,
    string InvoiceNumber,
    string CustomerName,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    InvoiceStatus Status,
    string Currency,
    decimal Amount,
    IReadOnlyList<InvoiceLineItemResult> LineItems,
    string? Notes);

/// <summary>The customer's read-only view, reached through the link in their email; no sign-in.</summary>
public record GetPublicInvoiceQuery(string Token) : IQuery<PublicInvoiceResult>;
