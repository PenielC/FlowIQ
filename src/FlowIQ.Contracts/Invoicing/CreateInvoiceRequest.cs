namespace FlowIQ.Contracts.Invoicing;

public record InvoiceLineItemRequest(string Description, decimal Amount);

public record CreateInvoiceRequest(
    string CustomerName,
    IReadOnlyCollection<InvoiceLineItemRequest> LineItems,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Currency,
    decimal? ExchangeRate,
    string? Notes);
