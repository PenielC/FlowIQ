using FlowIQ.Domain.Invoicing;
using Mediator;

namespace FlowIQ.Application.Invoicing.Commands.CreateInvoice;

public record CreateInvoiceCommand(
    Guid CompanyId,
    string CustomerName,
    decimal Amount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Currency,
    decimal? ExchangeRate) : ICommand<InvoiceResult>;
