using FlowIQ.Application.Invoicing.Emails;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.Invoicing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

/// <summary>The page a customer opens from "View invoice" in their email. The link's token is the only key.</summary>
[ApiController]
[Route("api/public/invoices")]
[AllowAnonymous]
public class PublicInvoicesController(ISender sender) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<ApiResponse<PublicInvoiceResponse>>> Get(string token, CancellationToken cancellationToken)
    {
        var r = await sender.Send(new GetPublicInvoiceQuery(token), cancellationToken);
        return Ok(ApiResponse<PublicInvoiceResponse>.Ok(new PublicInvoiceResponse(
            r.BusinessName, r.LogoDataUrl, r.InvoiceNumber, r.CustomerName, r.IssueDateUtc, r.DueDateUtc, r.Status.ToString(),
            r.Currency, r.Amount, r.LineItems.Select(li => new InvoiceLineItemResponse(li.Id, li.Description, li.Amount)).ToList(), r.Notes)));
    }
}
