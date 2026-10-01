using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Invoicing;

public record InvoiceLineItemResult(Guid Id, string Description, decimal Amount);

public record InvoiceResult(
    Guid Id,
    string CustomerName,
    decimal Amount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    InvoiceStatus Status,
    string Currency,
    decimal AmountInReportingCurrency,
    IReadOnlyCollection<InvoiceLineItemResult> LineItems,
    string? Notes,
    string? CustomerEmail = null,
    bool RemindersPaused = false)
{
    public static InvoiceResult From(Invoice i) => new(
        i.Id, i.CustomerName, i.Amount, i.IssueDateUtc, i.DueDateUtc, i.Status, i.Currency, i.AmountInReportingCurrency,
        i.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(),
        i.Notes, i.CustomerEmail, i.RemindersPaused);
}
