using Mediator;

namespace FlowIQ.Application.Invoicing.Queries.GetInvoiceSummary;

public record GetInvoiceSummaryQuery(Guid CompanyId) : IQuery<InvoiceSummaryResult>;

public record InvoiceSummaryResult(
    decimal TotalOutstanding,
    int CustomerCount,
    IReadOnlyCollection<InvoiceResult> UpcomingInvoices);
