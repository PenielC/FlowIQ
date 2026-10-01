using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Commands.CreateInvoice;
using FlowIQ.Application.Invoicing.Emails;
using FlowIQ.Application.Invoicing.Commands.MarkInvoicePaid;
using FlowIQ.Application.Invoicing.Queries.GetInvoiceById;
using FlowIQ.Application.Invoicing.Queries.GetInvoiceSummary;
using FlowIQ.Application.Invoicing.Queries.GetInvoices;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.Invoicing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceResponse>>>> GetInvoices(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInvoicesQuery(CurrentCompanyId, pageNumber, pageSize), cancellationToken);

        var response = new PagedResult<InvoiceResponse>
        {
            Items = result.Items.Select(ToResponse).ToList(),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
        };

        return Ok(ApiResponse<PagedResult<InvoiceResponse>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(CurrentCompanyId, id), cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(ToResponse(result)));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<InvoiceSummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceSummaryQuery(CurrentCompanyId), cancellationToken);

        var response = new InvoiceSummaryResponse(
            result.TotalOutstanding,
            result.CustomerCount,
            result.UpcomingInvoices.Select(ToResponse).ToList());

        return Ok(ApiResponse<InvoiceSummaryResponse>.Ok(response));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInvoiceCommand(
            CurrentCompanyId, request.CustomerName,
            request.LineItems.Select(li => new InvoiceLineItemInput(li.Description, li.Amount)).ToList(),
            request.IssueDateUtc, request.DueDateUtc,
            request.Currency, request.ExchangeRate, request.Notes, request.CustomerEmail);
        var result = await sender.Send(command, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkInvoicePaidCommand(CurrentCompanyId, id), cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(ToResponse(result)));
    }

    /// <summary>Emails the invoice to the customer, with a link to view and download it.</summary>
    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<ApiResponse<InvoiceEmailResponse>>> Send(Guid id, SendInvoiceEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendInvoiceEmailCommand(CurrentCompanyId, CurrentUserId, id, request.ToEmail, request.Message), cancellationToken);
        return Ok(ApiResponse<InvoiceEmailResponse>.Ok(ToResponse(result)));
    }

    /// <summary>Sets the customer email and whether this invoice is left out of reminders.</summary>
    [HttpPut("{id:guid}/delivery")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateDelivery(Guid id, UpdateInvoiceDeliveryRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateInvoiceDeliveryCommand(CurrentCompanyId, id, request.CustomerEmail, request.RemindersPaused), cancellationToken);
        return Ok(ApiResponse<InvoiceResponse>.Ok(ToResponse(result)));
    }

    /// <summary>The invoice's email history: when it was sent, and each reminder.</summary>
    [HttpGet("{id:guid}/emails")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceEmailResponse>>>> Emails(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceEmailsQuery(CurrentCompanyId, id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InvoiceEmailResponse>>.Ok(result.Select(ToResponse).ToList()));
    }

    [HttpGet("reminder-settings")]
    public async Task<ActionResult<ApiResponse<ReminderSettingsResponse>>> GetReminderSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReminderSettingsQuery(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<ReminderSettingsResponse>.Ok(new ReminderSettingsResponse(result.Enabled, result.Days)));
    }

    [HttpPut("reminder-settings")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<ReminderSettingsResponse>>> UpdateReminderSettings(ReminderSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateReminderSettingsCommand(CurrentCompanyId, request.Enabled, request.Days ?? []), cancellationToken);
        return Ok(ApiResponse<ReminderSettingsResponse>.Ok(new ReminderSettingsResponse(result.Enabled, result.Days)));
    }

    /// <summary>Sends any reminders that are due now, rather than waiting for the next scheduled run.</summary>
    [HttpPost("reminders/run")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<ReminderRunResponse>>> RunReminders(CancellationToken cancellationToken)
    {
        var r = await sender.Send(new RunInvoiceRemindersCommand(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<ReminderRunResponse>.Ok(new ReminderRunResponse(r.MarkedOverdue, r.Sent, r.Failed, r.Skipped)));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

    private static InvoiceEmailResponse ToResponse(InvoiceEmailResult e) =>
        new(e.Id, e.Kind.ToString(), e.ReminderDay, e.ToEmail, e.Status.ToString(), e.Error, e.AtUtc);

    private static InvoiceResponse ToResponse(InvoiceResult result) => new(
        result.Id, result.CustomerName, result.Amount, result.IssueDateUtc, result.DueDateUtc, result.Status.ToString(),
        result.Currency, result.AmountInReportingCurrency,
        result.LineItems.Select(li => new InvoiceLineItemResponse(li.Id, li.Description, li.Amount)).ToList(),
        result.Notes,
        result.CustomerEmail,
        result.RemindersPaused);
}
