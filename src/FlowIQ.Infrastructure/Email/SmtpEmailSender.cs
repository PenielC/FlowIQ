using System.Net;
using System.Net.Mail;
using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowIQ.Infrastructure.Email;

/// <summary>
/// Plain SMTP, used instead of Resend when <c>Email:SmtpHost</c> is set: locally that points at a catch-all
/// inbox such as Mailpit, so nothing reaches real customers while testing. Throws on failure, like Resend.
/// </summary>
public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        SendAsync(new OutgoingEmail(toEmail, subject, htmlBody), cancellationToken);

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        var fromAddress = SenderName.BareAddress(configuration["Email:FromAddress"] is { Length: > 0 } a ? a : "noreply@finflow.local");
        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, email.FromName is null ? "FinFlow" : SenderName.Clean(email.FromName)),
            Subject = email.Subject,
        };
        message.To.Add(email.ToEmail);
        if (email.ReplyTo is not null) message.ReplyToList.Add(email.ReplyTo);
        if (email.TextBody is null)
        {
            message.Body = email.HtmlBody;
            message.IsBodyHtml = true;
        }
        else
        {
            // Both parts as explicit alternatives (plain first, HTML last = preferred). Mixing Body with an
            // alternate view makes SmtpClient label the HTML body as text/plain.
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.TextBody, System.Text.Encoding.UTF8, "text/plain"));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.HtmlBody, System.Text.Encoding.UTF8, "text/html"));
        }

        using var client = new SmtpClient(configuration["Email:SmtpHost"], int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 25)
        {
            EnableSsl = configuration["Email:SmtpSsl"] == "true",
        };
        if (configuration["Email:SmtpUser"] is { Length: > 0 } user)
        {
            client.Credentials = new NetworkCredential(user, configuration["Email:SmtpPassword"]);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
