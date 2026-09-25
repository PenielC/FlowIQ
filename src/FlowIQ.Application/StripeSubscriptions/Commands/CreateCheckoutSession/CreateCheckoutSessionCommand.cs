using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Commands.CreateCheckoutSession;

public record CreateCheckoutSessionCommand(Guid CompanyId, string UserEmail, string CompanyName, string PlanKey)
    : ICommand<CheckoutSessionResult>;

public record CheckoutSessionResult(string CheckoutUrl);
