namespace FlowIQ.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>Sends with a sender display name (e.g. the business) and an address that replies go to.</summary>
    Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}

/// <param name="FromName">Shown as the sender, e.g. "Tatenda Foods via FinFlow"; the address stays FinFlow's.</param>
/// <param name="ReplyTo">Where the customer's reply goes, e.g. the person who sent the invoice.</param>
public record OutgoingEmail(
    string ToEmail,
    string Subject,
    string HtmlBody,
    string? TextBody = null,
    string? FromName = null,
    string? ReplyTo = null);
