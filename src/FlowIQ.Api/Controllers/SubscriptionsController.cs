using System.IdentityModel.Tokens.Jwt;
using FlowIQ.Application.StripeSubscriptions.Commands.CreateBillingPortalSession;
using FlowIQ.Application.StripeSubscriptions.Commands.CreateCheckoutSession;
using FlowIQ.Application.StripeSubscriptions.Commands.ProcessStripeWebhook;
using FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionPlans;
using FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionStatus;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.StripeSubscriptions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController(ISender sender, ILogger<SubscriptionsController> logger) : ControllerBase
{
    [HttpGet("plans")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<SubscriptionPlanResponse>>>> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await sender.Send(new GetSubscriptionPlansQuery(), cancellationToken);

        var response = plans.Select(p => new SubscriptionPlanResponse(p.Key, p.DisplayName, p.MonthlyPriceUsd, p.TrialDays, p.Features)).ToList();

        return Ok(ApiResponse<List<SubscriptionPlanResponse>>.Ok(response));
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<SubscriptionStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSubscriptionStatusQuery(CurrentCompanyId), cancellationToken);

        var response = new SubscriptionStatusResponse(
            result.HasSubscription, result.PlanKey, result.PlanDisplayName, result.MonthlyPriceUsd,
            result.Status?.ToString(), result.CurrentPeriodEndUtc);

        return Ok(ApiResponse<SubscriptionStatusResponse>.Ok(response));
    }

    [HttpPost("checkout-session")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<CheckoutSessionResponse>>> CreateCheckoutSession(
        CheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)!.Value;
        var name = User.FindFirst("name")?.Value ?? email;

        var result = await sender.Send(
            new CreateCheckoutSessionCommand(CurrentCompanyId, email, name, request.PlanKey), cancellationToken);

        return Ok(ApiResponse<CheckoutSessionResponse>.Ok(new CheckoutSessionResponse(result.CheckoutUrl)));
    }

    [HttpPost("billing-portal-session")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<PortalSessionResponse>>> CreateBillingPortalSession(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBillingPortalSessionCommand(CurrentCompanyId), cancellationToken);

        return Ok(ApiResponse<PortalSessionResponse>.Ok(new PortalSessionResponse(result.PortalUrl)));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            await sender.Send(new ProcessStripeWebhookCommand(body, signature), cancellationToken);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Rejected a Stripe webhook with an invalid signature.");
            return BadRequest();
        }

        return Ok();
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);
}
