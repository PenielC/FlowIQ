using Mediator;

namespace FlowIQ.Application.Invoicing.Commands.CreateInvoice;

public record InvoiceLineItemInput(string Description, decimal Amount);

public record CreateInvoiceCommand(
    Guid CompanyId,
    string CustomerName,
    IReadOnlyCollection<InvoiceLineItemInput> LineItems,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Currency,
    decimal? ExchangeRate,
    string? Notes) : ICommand<InvoiceResult>;
