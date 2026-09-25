namespace FlowIQ.Contracts.Invoicing;

public record CreateInvoiceRequest(
    string CustomerName,
    decimal Amount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Currency,
    decimal? ExchangeRate);
