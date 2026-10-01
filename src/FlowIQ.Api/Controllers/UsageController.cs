using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowIQ.Application.Analytics;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

public record RecordUsageRequest(string Feature);

/// <summary>The web app reports which page a signed-in user opened. Unknown page names are ignored.</summary>
[ApiController]
[Route("api/usage")]
[Authorize]
public class UsageController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record(RecordUsageRequest request, CancellationToken cancellationToken)
    {
        var companyId = Guid.Parse(User.FindFirst("company_id")!.Value);
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await sender.Send(new RecordUsageCommand(companyId, userId, request.Feature ?? string.Empty), cancellationToken);
        return NoContent();
    }
}
