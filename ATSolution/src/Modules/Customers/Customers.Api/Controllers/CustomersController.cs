using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Customers.Application.Abstractions;
using Customers.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Customers.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Customers.Base)]
[ApiController]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CustomerDto>>> List(
        [FromQuery] CustomerListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _customerService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(
        Guid id,
        [FromBody] UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _customerService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Customer deleted successfully."));
    }
}
