using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowIQ.Application.Analytics;
using FlowIQ.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FlowIQ.Api.Analytics;

/// <summary>
/// Counts a successful call to this endpoint as a use of <see cref="Feature"/> (see UsageFeatures), for the
/// admin usage view. Recorded only after a 2xx response, and never allowed to fail the request.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TrackUsageAttribute(string feature) : Attribute, IAsyncActionFilter
{
    public string Feature { get; } = feature;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        var http = context.HttpContext;
        if (executed.Exception is not null && !executed.ExceptionHandled) return;
        if (executed.Result is Microsoft.AspNetCore.Mvc.ObjectResult { StatusCode: >= 300 } or Microsoft.AspNetCore.Mvc.StatusCodeResult { StatusCode: >= 300 }) return;

        var user = http.User;
        var companyClaim = user.FindFirst("company_id")?.Value;
        var userClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(companyClaim, out var companyId) || !Guid.TryParse(userClaim, out var userId)) return;

        try
        {
            var store = http.RequestServices.GetRequiredService<IUsageStore>();
            var clock = http.RequestServices.GetRequiredService<IDateTimeProvider>();
            await store.RecordAsync(companyId, userId, Feature, clock.UtcNow, CancellationToken.None);
        }
        catch (Exception ex)
        {
            http.RequestServices.GetRequiredService<ILogger<TrackUsageAttribute>>()
                .LogWarning(ex, "Could not record usage of {Feature}.", Feature);
        }
    }
}
