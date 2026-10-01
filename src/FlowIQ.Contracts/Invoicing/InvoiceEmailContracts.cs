namespace FlowIQ.Contracts.Invoicing;

/// <param name="ToEmail">Optional: saved as the invoice's customer email when given.</param>
/// <param name="Message">Optional: replaces the default opening line.</param>
public record SendInvoiceEmailRequest(string? ToEmail, string? Message);

public record UpdateInvoiceDeliveryRequest(string? CustomerEmail, bool RemindersPaused);

/// <param name="Kind">"Invoice" or "Reminder".</param>
/// <param name="Status">"Sent", "Failed" or "Skipped".</param>
public record InvoiceEmailResponse(Guid Id, string Kind, int? ReminderDay, string ToEmail, string Status, string? Error, DateTime AtUtc);

public record ReminderSettingsRequest(bool Enabled, IReadOnlyList<int> Days);

public record ReminderSettingsResponse(bool Enabled, IReadOnlyList<int> Days);

public record ReminderRunResponse(int MarkedOverdue, int Sent, int Failed, int Skipped);

public record PublicInvoiceResponse(
    string BusinessName,
    string? LogoDataUrl,
    string InvoiceNumber,
    string CustomerName,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Status,
    string Currency,
    decimal Amount,
    IReadOnlyCollection<InvoiceLineItemResponse> LineItems,
    string? Notes);
