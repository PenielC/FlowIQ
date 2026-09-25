using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.BankTransactions.Commands.CreateTransaction;
using FlowIQ.Application.BankTransactions.Queries.GetDashboardSummary;
using FlowIQ.Application.BankTransactions.Queries.GetTransactions;
using FlowIQ.Application.BankTransactions.TransactionImport;
using FlowIQ.Application.BankTransactions.TransactionImport.Commands.ConfirmTransactionImport;
using FlowIQ.Application.BankTransactions.TransactionImport.Queries.PreviewTransactionImport;
using FlowIQ.Contracts.BankTransactions;
using FlowIQ.Contracts.Common;
using FlowIQ.Domain.BankTransactions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

/// <summary>Form-field shape for the multipart CSV import preview request — binds alongside a plain IFormFile parameter.</summary>
public class PreviewTransactionImportFormRequest
{
    public int DateColumnIndex { get; set; }
    public int DescriptionColumnIndex { get; set; }
    public int AmountColumnIndex { get; set; }
    public string DateFormat { get; set; } = string.Empty;
    public bool HasHeaderRow { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? ExchangeRate { get; set; }
}

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionResponse>>>> GetTransactions(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTransactionsQuery(CurrentCompanyId, pageNumber, pageSize), cancellationToken);

        var response = new PagedResult<TransactionResponse>
        {
            Items = result.Items.Select(ToResponse).ToList(),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
        };

        return Ok(ApiResponse<PagedResult<TransactionResponse>>.Ok(response));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary(
        [FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery(CurrentCompanyId, startDateUtc, endDateUtc), cancellationToken);

        var response = new DashboardSummaryResponse(
            result.CashBalance,
            result.MonthlyRevenue,
            result.RevenueChangePercent,
            result.MonthlyExpenses,
            result.ExpensesChangePercent,
            result.RecentTransactions.Select(ToResponse).ToList());

        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(response));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Create(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TransactionCategory>(request.Category, ignoreCase: true, out var category))
        {
            return BadRequest(ApiResponse<TransactionResponse>.Fail(
                "Validation failed.", new Dictionary<string, string[]> { ["Category"] = [$"'{request.Category}' is not a valid category."] }));
        }

        if (!Enum.TryParse<TransactionStatus>(request.Status, ignoreCase: true, out var status))
        {
            return BadRequest(ApiResponse<TransactionResponse>.Fail(
                "Validation failed.", new Dictionary<string, string[]> { ["Status"] = [$"'{request.Status}' is not a valid status."] }));
        }

        var command = new CreateTransactionCommand(
            CurrentCompanyId, request.Description, category, request.Amount, request.TransactionDateUtc, status,
            request.Currency, request.ExchangeRate);

        var result = await sender.Send(command, cancellationToken);

        return Ok(ApiResponse<TransactionResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<ApiResponse<TransactionImportPreviewResponse>>> PreviewImport(
        IFormFile file, [FromForm] PreviewTransactionImportFormRequest request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var mapping = new CsvColumnMapping(
            request.DateColumnIndex, request.DescriptionColumnIndex, request.AmountColumnIndex, request.DateFormat, request.HasHeaderRow);

        var result = await sender.Send(
            new PreviewTransactionImportQuery(CurrentCompanyId, stream.ToArray(), mapping, request.Currency, request.ExchangeRate),
            cancellationToken);

        var response = new TransactionImportPreviewResponse(
            result.Rows.Select(r => new TransactionImportRowResponse(
                r.RowNumber, r.TransactionDateUtc, r.Description, r.Amount, r.SuggestedCategory.ToString(), r.IsDuplicate, r.ParseError)).ToList(),
            result.ExchangeRate,
            result.ImportableCount);

        return Ok(ApiResponse<TransactionImportPreviewResponse>.Ok(response));
    }

    [HttpPost("import/confirm")]
    public async Task<ActionResult<ApiResponse<ImportTransactionsResponse>>> ConfirmImport(
        ConfirmTransactionImportRequest request, CancellationToken cancellationToken)
    {
        var rows = new List<ImportRowInput>();
        foreach (var r in request.Rows)
        {
            if (!Enum.TryParse<TransactionCategory>(r.Category, ignoreCase: true, out var category))
            {
                return BadRequest(ApiResponse<ImportTransactionsResponse>.Fail(
                    "Validation failed.", new Dictionary<string, string[]> { ["Category"] = [$"'{r.Category}' is not a valid category."] }));
            }
            rows.Add(new ImportRowInput(r.TransactionDateUtc, r.Description, r.Amount, category));
        }

        var command = new ConfirmTransactionImportCommand(CurrentCompanyId, rows, request.Currency, request.ExchangeRate);
        var result = await sender.Send(command, cancellationToken);

        return Ok(ApiResponse<ImportTransactionsResponse>.Ok(new ImportTransactionsResponse(result.ImportedCount)));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);

    private static TransactionResponse ToResponse(TransactionResult result) => new(
        result.Id, result.Description, result.Category.ToString(), result.Amount, result.TransactionDateUtc, result.Status.ToString(),
        result.Currency, result.AmountInReportingCurrency);
}
