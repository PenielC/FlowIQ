using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid CompanyId, Guid InvoiceId) : IQuery<InvoiceResult>;
