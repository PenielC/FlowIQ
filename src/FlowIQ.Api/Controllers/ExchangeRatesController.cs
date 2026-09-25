using FlowIQ.Application.ExchangeRates.Queries.GetExchangeRate;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.ExchangeRates;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
[Authorize]
public class ExchangeRatesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ExchangeRateResponse>>> GetRate(
        [FromQuery] string from, [FromQuery] string to, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExchangeRateQuery(from, to), cancellationToken);

        return Ok(ApiResponse<ExchangeRateResponse>.Ok(new ExchangeRateResponse(result.Rate, result.IsLive)));
    }
}
