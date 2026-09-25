using FlowIQ.Application.CompaniesAndTeams.Commands.RemoveCompanyLogo;
using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FlowIQ.Application.CompaniesAndTeams.Commands.UploadCompanyLogo;
using FlowIQ.Application.CompaniesAndTeams.Queries.GetCompanyLogo;
using FlowIQ.Contracts.CompaniesAndTeams;
using FlowIQ.Contracts.Common;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController(ISender sender) : ControllerBase
{
    [HttpGet("logo")]
    public async Task<ActionResult<ApiResponse<CompanyLogoResponse>>> GetLogo(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCompanyLogoQuery(CurrentCompanyId), cancellationToken);

        var dataUrl = result.Data is not null && result.ContentType is not null
            ? $"data:{result.ContentType};base64,{Convert.ToBase64String(result.Data)}"
            : null;

        return Ok(ApiResponse<CompanyLogoResponse>.Ok(new CompanyLogoResponse(dataUrl)));
    }

    [HttpPost("logo")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        await sender.Send(new UploadCompanyLogoCommand(CurrentCompanyId, stream.ToArray(), file.ContentType), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("logo")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveLogo(CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveCompanyLogoCommand(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPut("currency")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCurrency(UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCompanyCurrencyCommand(CurrentCompanyId, request.Currency), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);
}
