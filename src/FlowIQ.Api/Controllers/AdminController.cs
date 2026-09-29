using System.IdentityModel.Tokens.Jwt;
using FlowIQ.Application.Admin.Commands.RepairCurrencyData;
using FlowIQ.Application.Admin.Commands.SetCompanyActiveStatus;
using FlowIQ.Application.Admin.Commands.UpdateCompany;
using FlowIQ.Application.Admin.Queries.GetAdminOverview;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Contracts.Admin;
using FlowIQ.Contracts.Common;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController(ISender sender, IPlatformAdminChecker platformAdminChecker) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<AdminOverviewResponse>>> GetOverview(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        if (EnsureAdmin() is { } forbid) return forbid;

        var result = await sender.Send(new GetAdminOverviewQuery(pageNumber, pageSize, status, year, month), cancellationToken);

        var response = new AdminOverviewResponse(
            result.TotalCompanies,
            result.NewCompaniesThisMonth,
            result.TotalSubscriptions,
            result.NewSubscriptionsThisMonth,
            result.UsageByPlatform,
            result.StatusCounts,
            new PagedResult<AdminCompanyRowResponse>
            {
                Items = result.Companies.Select(ToResponse).ToList(),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.FilteredCount,
            },
            result.ReportYear,
            result.ReportMonth,
            result.MonthlyTrend.Select(t => new AdminMonthlyTrendPointResponse(t.Year, t.Month, t.NewCompanies, t.NewSubscriptions)).ToList());

        return Ok(ApiResponse<AdminOverviewResponse>.Ok(response));
    }

    [HttpPut("companies/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCompany(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;

        await sender.Send(new UpdateCompanyCommand(id, request.Name, request.Currency), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("companies/{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateCompany(Guid id, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;

        await sender.Send(new SetCompanyActiveStatusCommand(id, true), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("companies/{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateCompany(Guid id, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;

        await sender.Send(new SetCompanyActiveStatusCommand(id, false), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    /// <summary>One-off repair of currency data written before the conversion fixes. Dry run by default.</summary>
    [HttpPost("repair-currency-data")]
    public async Task<ActionResult<ApiResponse<RepairCurrencyDataResponse>>> RepairCurrencyData(
        RepairCurrencyDataRequest request, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;

        var r = await sender.Send(new RepairCurrencyDataCommand(request.DryRun, request.BackfillPaidInvoiceIncome), cancellationToken);
        return Ok(ApiResponse<RepairCurrencyDataResponse>.Ok(new RepairCurrencyDataResponse(
            r.DryRun,
            r.Companies.Select(c => new CompanyRepairRowResponse(
                c.CompanyId, c.CompanyName, c.Currency, c.TransactionsRestated, c.InvoicesRestated, c.IncomeRecorded, c.Problem)).ToList(),
            r.TransactionsRestated,
            r.InvoicesRestated,
            r.IncomeRecorded)));
    }

    private ActionResult? EnsureAdmin()
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)!.Value;
        return platformAdminChecker.IsPlatformAdmin(email) ? null : Forbid();
    }

    private static AdminCompanyRowResponse ToResponse(AdminCompanyRow row) => new(
        row.Id, row.Name, row.Currency, row.CreatedAtUtc, row.OwnerName, row.OwnerEmail, row.SubscriptionStatus, row.PlanKey, row.IsActive);
}
