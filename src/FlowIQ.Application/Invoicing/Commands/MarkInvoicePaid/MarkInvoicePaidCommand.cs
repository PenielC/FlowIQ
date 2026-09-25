using Mediator;

namespace FlowIQ.Application.Invoicing.Commands.MarkInvoicePaid;

public record MarkInvoicePaidCommand(Guid CompanyId, Guid InvoiceId) : ICommand<InvoiceResult>;
