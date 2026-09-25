using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Domain.StripeSubscriptions;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using StripeSubscription = Stripe.Subscription;

namespace FlowIQ.Infrastructure.StripeSubscriptions;

public class StripeGateway : IStripeGateway
{
    private readonly StripeClient _client;
    private readonly string _webhookSecret;

    public StripeGateway(IConfiguration configuration)
    {
        var secretKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
        _client = new StripeClient(secretKey);
    }

    public async Task<string> GetOrCreateCustomerAsync(
        Guid companyId, string email, string companyName, CancellationToken cancellationToken = default)
    {
        var service = new CustomerService(_client);
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = companyName,
            Metadata = new Dictionary<string, string> { ["company_id"] = companyId.ToString() },
        }, cancellationToken: cancellationToken);

        return customer.Id;
    }

    public async Task<CheckoutSessionInfo> CreateCheckoutSessionAsync(
        string stripeCustomerId, string planLookupKey, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var priceId = await GetOrCreatePriceIdAsync(planLookupKey, cancellationToken);

        var service = new SessionService(_client);
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            Mode = "subscription",
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            // This account has Stripe's newer "Managed Payments" feature on by default, which requires
            // every product to carry a tax code we have no use for — opt this session out of it instead.
            ManagedPayments = new SessionManagedPaymentsOptions { Enabled = false },
        }, cancellationToken: cancellationToken);

        return new CheckoutSessionInfo(session.Url);
    }

    public async Task<PortalSessionInfo> CreateBillingPortalSessionAsync(
        string stripeCustomerId, string returnUrl, CancellationToken cancellationToken = default)
    {
        var service = new Stripe.BillingPortal.SessionService(_client);
        var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl,
        }, cancellationToken: cancellationToken);

        return new PortalSessionInfo(session.Url);
    }

    public StripeWebhookEvent ConstructEvent(string requestBody, string signatureHeader)
    {
        var stripeEvent = EventUtility.ConstructEvent(requestBody, signatureHeader, _webhookSecret);

        string? customerId = null;
        string? subscriptionId = null;
        string? planLookupKey = null;
        SubscriptionStatus? status = null;
        DateTime? currentPeriodEndUtc = null;

        if (stripeEvent.Data.Object is StripeSubscription subscription)
        {
            customerId = subscription.CustomerId;
            subscriptionId = subscription.Id;
            planLookupKey = subscription.Items.Data.FirstOrDefault()?.Price?.LookupKey;
            status = MapStatus(subscription.Status);
            currentPeriodEndUtc = subscription.Items.Data.FirstOrDefault()?.CurrentPeriodEnd;
        }

        return new StripeWebhookEvent(stripeEvent.Id, stripeEvent.Type, customerId, subscriptionId, planLookupKey, status, currentPeriodEndUtc);
    }

    private async Task<string> GetOrCreatePriceIdAsync(string lookupKey, CancellationToken cancellationToken)
    {
        var priceService = new PriceService(_client);
        var existing = await priceService.ListAsync(new PriceListOptions
        {
            LookupKeys = [lookupKey],
            Active = true,
            Limit = 1,
        }, cancellationToken: cancellationToken);

        var existingPrice = existing.Data.FirstOrDefault();
        if (existingPrice is not null)
        {
            return existingPrice.Id;
        }

        var plan = SubscriptionPlanCatalog.FindByLookupKey(lookupKey)
            ?? throw new InvalidOperationException($"No plan configured for lookup key '{lookupKey}'.");

        var productService = new ProductService(_client);
        var product = await productService.CreateAsync(new ProductCreateOptions
        {
            Name = $"FlowIQ {plan.DisplayName}",
        }, cancellationToken: cancellationToken);

        var price = await priceService.CreateAsync(new PriceCreateOptions
        {
            Product = product.Id,
            UnitAmount = (long)(plan.MonthlyPriceUsd * 100),
            Currency = "usd",
            Recurring = new PriceRecurringOptions { Interval = "month" },
            LookupKey = lookupKey,
        }, cancellationToken: cancellationToken);

        return price.Id;
    }

    private static SubscriptionStatus MapStatus(string stripeStatus) => stripeStatus switch
    {
        "trialing" => SubscriptionStatus.Trialing,
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Canceled,
        "unpaid" => SubscriptionStatus.Unpaid,
        "incomplete_expired" => SubscriptionStatus.Canceled,
        _ => SubscriptionStatus.Incomplete,
    };
}
