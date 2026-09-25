using FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;
using FlowIQ.Contracts.CashFlowForecasting;
using FlowIQ.Contracts.Common;
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
            result.Points.Select(p => new CashFlowPointResponse(p.DateUtc, p.Actual, p.Forecast)).ToList());

        return Ok(ApiResponse<CashFlowForecastResponse>.Ok(response));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);
}
