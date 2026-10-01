namespace FlowIQ.Contracts.Invoicing;

public record InvoiceLineItemResponse(Guid Id, string Description, decimal Amount);

public record InvoiceResponse(
    Guid Id,
    string CustomerName,
    decimal Amount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Status,
    string Currency,
    decimal AmountInReportingCurrency,
    IReadOnlyCollection<InvoiceLineItemResponse> LineItems,
    string? Notes,
    string? CustomerEmail = null,
    bool RemindersPaused = false);
