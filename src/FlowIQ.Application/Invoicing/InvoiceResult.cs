using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Invoicing;

public record InvoiceResult(
    Guid Id,
    string CustomerName,
    decimal Amount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    InvoiceStatus Status,
    string Currency,
    decimal AmountInReportingCurrency);
