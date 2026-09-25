using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Commands.CreateInvoice;
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
            CurrentCompanyId, request.CustomerName, request.Amount, request.IssueDateUtc, request.DueDateUtc,
            request.Currency, request.ExchangeRate);
        var result = await sender.Send(command, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkInvoicePaidCommand(CurrentCompanyId, id), cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(ToResponse(result)));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);

    private static InvoiceResponse ToResponse(InvoiceResult result) => new(
        result.Id, result.CustomerName, result.Amount, result.IssueDateUtc, result.DueDateUtc, result.Status.ToString(),
        result.Currency, result.AmountInReportingCurrency);
}
