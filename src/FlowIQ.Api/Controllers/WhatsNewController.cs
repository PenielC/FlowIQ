using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowIQ.Application.ProductUpdates;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.ProductUpdates;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

/// <summary>The signed-in user's "What's new" feed and dashboard feature cards.</summary>
[ApiController]
[Route("api/whats-new")]
[Authorize]
public class WhatsNewController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WhatsNewResponse>>> Get(CancellationToken cancellationToken)
    {
        var r = await sender.Send(new GetWhatsNewQuery(CurrentUserId), cancellationToken);
        return Ok(ApiResponse<WhatsNewResponse>.Ok(new WhatsNewResponse(
            r.Updates.Select(ToResponse).ToList(), r.UnreadCount, r.DashboardCards.Select(ToResponse).ToList())));
    }

    /// <summary>The user opened the feed: everything published so far counts as read.</summary>
    [HttpPost("seen")]
    public async Task<ActionResult<ApiResponse<object>>> MarkSeen(CancellationToken cancellationToken)
    {
        await sender.Send(new MarkWhatsNewSeenCommand(CurrentUserId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    /// <summary>Hides an update's dashboard card for this user, on every device.</summary>
    [HttpPost("{id:guid}/dismiss")]
    public async Task<ActionResult<ApiResponse<object>>> Dismiss(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DismissProductUpdateCommand(CurrentUserId, id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    internal static ProductUpdateResponse ToResponse(ProductUpdateResult u) =>
        new(u.Id, u.Title, u.Summary, u.LinkUrl, u.LinkLabel, u.Audience.ToString(), u.ShowOnDashboard, u.PublishedAtUtc, u.IsUnread);
}
