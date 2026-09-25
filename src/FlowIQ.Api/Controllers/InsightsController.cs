using FlowIQ.Application.AiInsights.Queries.GetAiInsights;
using FlowIQ.Contracts.AiInsights;
using FlowIQ.Contracts.Common;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/insights")]
[Authorize]
public class InsightsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AiInsightsResponse>>> GetInsights(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAiInsightsQuery(CurrentCompanyId), cancellationToken);

        var response = new AiInsightsResponse(
            result.Insights.Select(i => new InsightResponse(i.Title, i.Description, i.Tone.ToString())).ToList());

        return Ok(ApiResponse<AiInsightsResponse>.Ok(response));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);
}
