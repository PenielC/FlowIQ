using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Commands.CreateBillingPortalSession;

public record CreateBillingPortalSessionCommand(Guid CompanyId) : ICommand<PortalSessionResult>;

public record PortalSessionResult(string PortalUrl);
