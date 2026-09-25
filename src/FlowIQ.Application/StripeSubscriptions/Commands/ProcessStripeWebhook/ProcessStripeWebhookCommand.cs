using Mediator;

namespace FlowIQ.Application.StripeSubscriptions.Commands.ProcessStripeWebhook;

public record ProcessStripeWebhookCommand(string RequestBody, string SignatureHeader) : ICommand<WebhookProcessResult>;

public record WebhookProcessResult(bool Processed);
