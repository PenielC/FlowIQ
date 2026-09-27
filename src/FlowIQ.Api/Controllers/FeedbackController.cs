using System.IdentityModel.Tokens.Jwt;
using FlowIQ.Application.Feedback.Commands.SendFeedback;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.Feedback;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Send(FeedbackRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        await sender.Send(new SendFeedbackCommand(userId, request.Message), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
