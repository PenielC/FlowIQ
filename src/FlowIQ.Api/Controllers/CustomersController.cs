using FlowIQ.Application.Customers;
using FlowIQ.Application.Customers.Commands.CreateCustomer;
using FlowIQ.Application.Customers.Commands.DeleteCustomer;
using FlowIQ.Application.Customers.Commands.UpdateCustomer;
using FlowIQ.Application.Customers.Queries.GetCustomerById;
using FlowIQ.Application.Customers.Queries.GetCustomers;
using FlowIQ.Contracts.Common;
using FlowIQ.Contracts.Customers;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerResponse>>>> GetCustomers(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCustomersQuery(CurrentCompanyId, pageNumber, pageSize), cancellationToken);

        var response = new PagedResult<CustomerResponse>
        {
            Items = result.Items.Select(ToResponse).ToList(),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
        };

        return Ok(ApiResponse<PagedResult<CustomerResponse>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerByIdQuery(CurrentCompanyId, id), cancellationToken);
        return Ok(ApiResponse<CustomerResponse>.Ok(ToResponse(result)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(CurrentCompanyId, request.Name, request.Email, request.Phone, request.Notes);
        var result = await sender.Send(command, cancellationToken);

        return Ok(ApiResponse<CustomerResponse>.Ok(ToResponse(result)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(CurrentCompanyId, id, request.Name, request.Email, request.Phone, request.Notes);
        var result = await sender.Send(command, cancellationToken);

        return Ok(ApiResponse<CustomerResponse>.Ok(ToResponse(result)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCustomerCommand(CurrentCompanyId, id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);

    private static CustomerResponse ToResponse(CustomerResult result) => new(
        result.Id, result.Name, result.Email, result.Phone, result.Notes, result.CreatedAtUtc);
}
