namespace FlowIQ.Contracts.Invoicing;

public record InvoiceResponse(
    Guid Id,
    string CustomerName,
    decimal Amount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Status,
    string Currency,
    decimal AmountInReportingCurrency);
