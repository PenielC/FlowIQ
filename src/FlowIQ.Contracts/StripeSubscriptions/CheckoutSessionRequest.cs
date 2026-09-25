namespace FlowIQ.Contracts.StripeSubscriptions;

public record CheckoutSessionRequest(string PlanKey);

public record CheckoutSessionResponse(string CheckoutUrl);

public record PortalSessionResponse(string PortalUrl);
