using FlowIQ.Application.CashFlowForecasting.OwnerDraws;
using FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;
using FlowIQ.Contracts.CashFlowForecasting;
using FlowIQ.Contracts.Common;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.Exceptions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/forecast")]
[Authorize]
public class ForecastController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CashFlowForecastResponse>>> GetForecast(
        [FromQuery] int historyDays = 30, [FromQuery] int forecastDays = 30, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCashFlowForecastQuery(CurrentCompanyId, historyDays, forecastDays), cancellationToken);

        var response = new CashFlowForecastResponse(
            result.Points.Select(p => new CashFlowPointResponse(p.DateUtc, p.Actual, p.Forecast)).ToList(),
            result.Events.Select(e => new ForecastEventResponse(e.DateUtc, e.Label, e.Amount, e.Kind.ToString())).ToList(),
            result.CurrentBalance,
            result.LowestBalance,
            result.LowestBalanceDateUtc);

        return Ok(ApiResponse<CashFlowForecastResponse>.Ok(response));
    }

    /// <summary>Planned personal withdrawals (school fees, home rent...) and whether the setup question was answered.</summary>
    [HttpGet("owner-draws")]
    public async Task<ActionResult<ApiResponse<OwnerDrawsResponse>>> GetOwnerDraws(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOwnerDrawsQuery(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<OwnerDrawsResponse>.Ok(new OwnerDrawsResponse(result.SetupCompleted, result.Draws.Select(ToResponse).ToList())));
    }

    [HttpPost("owner-draws")]
    public async Task<ActionResult<ApiResponse<OwnerDrawResponse>>> CreateOwnerDraw(SaveOwnerDrawRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOwnerDrawCommand(CurrentCompanyId, request.Name, request.Amount, request.Currency, request.ExchangeRate,
            ParseFrequency(request.Frequency), ToUtcDate(request.NextDateUtc), request.Months);
        var result = await sender.Send(command, cancellationToken);
        return Ok(ApiResponse<OwnerDrawResponse>.Ok(ToResponse(result)));
    }

    [HttpPut("owner-draws/{id:guid}")]
    public async Task<ActionResult<ApiResponse<OwnerDrawResponse>>> UpdateOwnerDraw(Guid id, SaveOwnerDrawRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateOwnerDrawCommand(CurrentCompanyId, id, request.Name, request.Amount, request.Currency, request.ExchangeRate,
            ParseFrequency(request.Frequency), ToUtcDate(request.NextDateUtc), request.Months);
        var result = await sender.Send(command, cancellationToken);
        return Ok(ApiResponse<OwnerDrawResponse>.Ok(ToResponse(result)));
    }

    [HttpDelete("owner-draws/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOwnerDraw(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteOwnerDrawCommand(CurrentCompanyId, id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    /// <summary>Records that the owner answered the setup (including "I take nothing out"), so the dashboard stops asking.</summary>
    [HttpPost("setup-complete")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteSetup(CancellationToken cancellationToken)
    {
        await sender.Send(new CompleteForecastSetupCommand(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);

    private static DrawFrequency ParseFrequency(string value) =>
        Enum.TryParse<DrawFrequency>(value, ignoreCase: true, out var frequency) && Enum.IsDefined(frequency)
            ? frequency
            : throw new DomainException("Frequency must be Once, Weekly, Monthly or SelectedMonths.");

    // Dates arrive as "2026-10-05" (Kind=Unspecified); the calendar day is what matters.
    private static DateTime ToUtcDate(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static OwnerDrawResponse ToResponse(OwnerDrawResult d) => new(
        d.Id, d.Name, d.Amount, d.Currency, d.AmountInReportingCurrency, d.Frequency.ToString(), d.NextDateUtc, d.Months);
}
