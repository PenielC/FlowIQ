namespace FlowIQ.Contracts.Invoicing;

public record InvoiceSummaryResponse(
    decimal TotalOutstanding,
    int CustomerCount,
    IReadOnlyCollection<InvoiceResponse> UpcomingInvoices);
