using FlowIQ.Domain.StripeSubscriptions;

namespace FlowIQ.Application.StripeSubscriptions;

public record CheckoutSessionInfo(string CheckoutUrl);

public record PortalSessionInfo(string PortalUrl);

/// <summary>A normalized view of a Stripe webhook event — keeps the Stripe SDK's own types out of Application.</summary>
public record StripeWebhookEvent(
    string EventId,
    string EventType,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    string? PlanLookupKey,
    SubscriptionStatus? Status,
    DateTime? CurrentPeriodEndUtc);

public interface IStripeGateway
{
    Task<string> GetOrCreateCustomerAsync(
        Guid companyId, string email, string companyName, CancellationToken cancellationToken = default);

    Task<CheckoutSessionInfo> CreateCheckoutSessionAsync(
        string stripeCustomerId, string planLookupKey, string successUrl, string cancelUrl, CancellationToken cancellationToken = default);

    Task<PortalSessionInfo> CreateBillingPortalSessionAsync(
        string stripeCustomerId, string returnUrl, CancellationToken cancellationToken = default);

    StripeWebhookEvent ConstructEvent(string requestBody, string signatureHeader);
}
