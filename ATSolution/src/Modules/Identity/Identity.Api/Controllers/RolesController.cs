using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Identity.Api.DTOs.Requests;
using Identity.Application.Abstractions;
using Identity.Application.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Identity.Roles)]
[ApiController]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RoleDto>>> List(
        [FromQuery] RoleListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _roleService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var role = await _roleService.CreateAsync(
            new CreateRoleCommand(
                request.Name,
                request.Status,
                request.Permissions ?? Array.Empty<string>(),
                request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(
        Guid id,
        [FromBody] UpdateRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var role = await _roleService.UpdateAsync(
            new UpdateRoleCommand(
                id,
                request.Name,
                request.Description,
                request.Permissions,
                request.Status),
            cancellationToken);

        return Ok(role);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
