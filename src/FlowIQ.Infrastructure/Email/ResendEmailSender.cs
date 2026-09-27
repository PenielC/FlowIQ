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
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var fromAddress = configuration["Email:FromAddress"] ?? "onboarding@resend.dev";

        var payload = new
        {
            from = fromAddress,
            to = new[] { toEmail },
            subject,
            html = htmlBody,
        };

        using var response = await httpClient.PostAsJsonAsync("emails", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Resend returned {response.StatusCode} sending to {toEmail}: {body}");
        }
    }
}
