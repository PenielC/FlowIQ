using FlowIQ.Application.AiCategorisation.Queries.SuggestCategory;
using FlowIQ.Contracts.AiCategorisation;
using FlowIQ.Contracts.Common;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/categorisation")]
[Authorize]
public class CategorisationController(ISender sender) : ControllerBase
{
    [HttpPost("suggest")]
    public async Task<ActionResult<ApiResponse<SuggestCategoryResponse>>> Suggest(
        SuggestCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SuggestCategoryQuery(request.Description), cancellationToken);

        return Ok(ApiResponse<SuggestCategoryResponse>.Ok(new SuggestCategoryResponse(result.Category.ToString(), result.Confidence)));
    }
}
