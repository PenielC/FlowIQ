using System.Globalization;
using System.Text;
using FlowIQ.Application.ReportsAndAnalytics.Queries.GetReportsSummary;
using FlowIQ.Application.ReportsAndAnalytics.Queries.GetTransactionsForExport;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.ReportsAndAnalytics;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ReportsSummaryResponse>>> GetSummary(
        [FromQuery] int monthsBack = 6, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetReportsSummaryQuery(CurrentCompanyId, monthsBack), cancellationToken);

        var response = new ReportsSummaryResponse(
            result.MonthlyTrend.Select(p => new MonthlyTrendPointResponse(p.Year, p.Month, p.Revenue, p.Expenses)).ToList(),
            result.CategoryBreakdown.Select(c => new CategoryTotalResponse(c.Category.ToString(), c.Total)).ToList(),
            result.InvoiceStatusBreakdown.Select(s => new InvoiceStatusTotalResponse(s.Status.ToString(), s.Count, s.TotalAmount)).ToList());

        return Ok(ApiResponse<ReportsSummaryResponse>.Ok(response));
    }

    [HttpGet("export/transactions")]
    public async Task<IActionResult> ExportTransactions(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken cancellationToken = default)
    {
        // Query-string-bound DateTimes come in as Kind=Unspecified — Npgsql requires UTC-kind values
        // for a timestamptz column, so this must be stamped explicitly when an explicit date is provided.
        var end = DateTime.SpecifyKind((endDate ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc);
        var start = DateTime.SpecifyKind((startDate ?? end.AddMonths(-3)).Date, DateTimeKind.Utc);

        var transactions = await sender.Send(new GetTransactionsForExportQuery(CurrentCompanyId, start, end), cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Date,Description,Category,Amount,Status");
        foreach (var t in transactions)
        {
            csv.AppendLine(string.Join(',',
                t.TransactionDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CsvEscape(t.Description),
                t.Category,
                t.Amount.ToString(CultureInfo.InvariantCulture),
                t.Status));
        }

        var fileName = $"transactions-{start:yyyy-MM-dd}-to-{end.AddDays(-1):yyyy-MM-dd}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);
}
