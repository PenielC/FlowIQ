using System.Globalization;
using System.Net;
using System.Text;
using FlowIQ.Domain.Invoicing;

namespace FlowIQ.Application.Invoicing.Emails;

public record ComposedEmail(string Subject, string Html, string Text);

/// <summary>
/// The invoice and reminder emails a customer receives: the invoice itself in the body (line items, total,
/// due date) and a button to the private view page where they can download the PDF. Everything that came
/// from a person (names, line items, the optional message) is HTML-escaped.
/// </summary>
public static class InvoiceEmailComposer
{
    public static string InvoiceNumber(Guid id) => $"INV-{id.ToString()[..8].ToUpperInvariant()}";

    public static string Money(decimal amount, string currency) => $"{currency} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

    private static string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    public static ComposedEmail Invoice(Invoice invoice, string businessName, string viewUrl, string? message)
    {
        var intro = string.IsNullOrWhiteSpace(message)
            ? $"Please find your invoice from {businessName} below. Thank you for your business."
            : message.Trim();
        var subject = $"Invoice {InvoiceNumber(invoice.Id)} from {businessName}: {Money(invoice.Amount, invoice.Currency)}, due {Day(invoice.DueDateUtc)}";
        return Build(invoice, businessName, viewUrl, subject, intro);
    }

    public static ComposedEmail Reminder(Invoice invoice, string businessName, string viewUrl, int daysOverdue)
    {
        var days = daysOverdue == 1 ? "1 day" : $"{daysOverdue} days";
        var intro =
            $"This is a friendly reminder that invoice {InvoiceNumber(invoice.Id)} for {Money(invoice.Amount, invoice.Currency)} " +
            $"was due on {Day(invoice.DueDateUtc)} and is now {days} overdue.\n\n" +
            "If you've already paid, thank you, and please ignore this email. If something is holding the payment up, just reply and let us know.";
        var subject = $"Reminder: invoice {InvoiceNumber(invoice.Id)} from {businessName} was due {Day(invoice.DueDateUtc)}";
        return Build(invoice, businessName, viewUrl, subject, intro);
    }

    private static ComposedEmail Build(Invoice invoice, string businessName, string viewUrl, string subject, string intro)
    {
        string E(string s) => WebUtility.HtmlEncode(s);
        var greeting = $"Hi {invoice.CustomerName.Trim()},";

        var rows = new StringBuilder();
        foreach (var line in invoice.LineItems)
        {
            rows.Append($"<tr><td style=\"padding:8px 0;border-bottom:1px solid #eef2f7\">{E(line.Description)}</td>")
                .Append($"<td style=\"padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;white-space:nowrap\">{E(Money(line.Amount, invoice.Currency))}</td></tr>");
        }

        var paragraphs = string.Join("", intro.Split("\n\n").Select(p => $"<p style=\"margin:0 0 14px;line-height:1.55\">{E(p).Replace("\n", "<br>")}</p>"));
        var html = $"""
            <!doctype html><html><body style="margin:0;background:#f1f5f9;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#0f172a">
            <div style="max-width:600px;margin:0 auto;padding:24px 16px">
            <div style="background:#ffffff;border-radius:14px;padding:28px;font-size:15px">
            <p style="margin:0 0 4px;font-size:18px;font-weight:700">{E(businessName)}</p>
            <p style="margin:0 0 20px;color:#64748b;font-size:13px">Invoice {E(InvoiceNumber(invoice.Id))} &middot; issued {E(Day(invoice.IssueDateUtc))} &middot; due {E(Day(invoice.DueDateUtc))}</p>
            <p style="margin:0 0 14px">{E(greeting)}</p>
            {paragraphs}
            <table style="width:100%;border-collapse:collapse;margin:8px 0 4px;font-size:14px">{rows}
            <tr><td style="padding:12px 0 0;font-weight:700">Total due</td><td style="padding:12px 0 0;text-align:right;font-weight:700;white-space:nowrap">{E(Money(invoice.Amount, invoice.Currency))}</td></tr></table>
            <p style="margin:24px 0 8px"><a href="{E(viewUrl)}" style="background:#10b981;color:#ffffff;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600;display:inline-block">View invoice</a></p>
            <p style="margin:0;color:#64748b;font-size:13px">You can download a PDF copy from that page. Reply to this email with any questions.</p>
            </div>
            <p style="text-align:center;color:#94a3b8;font-size:12px;margin:16px 0 0">Sent by {E(businessName)} with FinFlow</p>
            </div></body></html>
            """;

        var text = new StringBuilder()
            .AppendLine(greeting).AppendLine()
            .AppendLine(intro).AppendLine();
        foreach (var line in invoice.LineItems)
        {
            text.AppendLine($"- {line.Description}: {Money(line.Amount, invoice.Currency)}");
        }

        text.AppendLine($"Total due: {Money(invoice.Amount, invoice.Currency)} (due {Day(invoice.DueDateUtc)})").AppendLine()
            .AppendLine($"View or download the invoice: {viewUrl}").AppendLine()
            .AppendLine($"Sent by {businessName} with FinFlow");

        return new ComposedEmail(subject, html, text.ToString());
    }
}
