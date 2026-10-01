using System.Net.Http.Json;
using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowIQ.Infrastructure.Email;

/// <summary>
/// Sends transactional email via the Resend HTTP API. Unlike FrankfurterExchangeRateProvider (which swallows
/// failures and returns null), a failed send here throws — the caller (e.g. a password reset request) needs to
/// know delivery actually failed rather than silently proceeding as if the user received an email they didn't.
/// </summary>
public class ResendEmailSender(HttpClient httpClient, IConfiguration configuration) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        SendAsync(new OutgoingEmail(toEmail, subject, htmlBody), cancellationToken);

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        var fromAddress = configuration["Email:FromAddress"] ?? "onboarding@resend.dev";
        var from = email.FromName is null ? fromAddress : $"{SenderName.Clean(email.FromName)} <{SenderName.BareAddress(fromAddress)}>";

        var payload = new Dictionary<string, object>
        {
            ["from"] = from,
            ["to"] = new[] { email.ToEmail },
            ["subject"] = email.Subject,
            ["html"] = email.HtmlBody,
        };
        if (email.TextBody is not null) payload["text"] = email.TextBody;
        if (email.ReplyTo is not null) payload["reply_to"] = email.ReplyTo;

        using var response = await httpClient.PostAsJsonAsync("emails", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Resend returned {response.StatusCode} sending to {email.ToEmail}: {body}");
        }
    }
}

internal static class SenderName
{
    /// <summary>"FinFlow &lt;hi@x.com&gt;" or "hi@x.com" → "hi@x.com".</summary>
    public static string BareAddress(string configured)
    {
        var open = configured.IndexOf('<');
        var close = configured.IndexOf('>');
        return open >= 0 && close > open ? configured[(open + 1)..close].Trim() : configured.Trim();
    }

    /// <summary>A display name can't carry characters that would break the From header.</summary>
    public static string Clean(string name) => new(name.Where(c => c is not ('<' or '>' or '"' or '\r' or '\n')).ToArray());
}
