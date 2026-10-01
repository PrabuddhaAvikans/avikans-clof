using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Identity.Api.DTOs.Requests;
using Identity.Application.Abstractions;
using Identity.Application.RoleGroups;
using Identity.Application.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Identity.RoleGroups)]
[ApiController]
public sealed class RoleGroupsController : ControllerBase
{
    private readonly IRoleGroupService _roleGroupService;

    public RoleGroupsController(IRoleGroupService roleGroupService)
    {
        _roleGroupService = roleGroupService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RoleGroupDto>>> List(
        [FromQuery] RoleListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _roleGroupService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<RoleGroupDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var group = await _roleGroupService.GetByIdAsync(id, cancellationToken);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<RoleGroupDto>> Create(
        [FromBody] CreateRoleGroupRequestDto request,
        CancellationToken cancellationToken)
    {
        var group = await _roleGroupService.CreateAsync(
            new CreateRoleGroupCommand(
                request.Name,
                request.Status,
                request.RoleIds ?? Array.Empty<Guid>(),
                request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPut(ApiRoutes.ById)]
    public async Task<ActionResult<RoleGroupDto>> Update(
        Guid id,
        [FromBody] UpdateRoleGroupRequestDto request,
        CancellationToken cancellationToken)
    {
        var group = await _roleGroupService.UpdateAsync(
            new UpdateRoleGroupCommand(
                id,
                request.Name,
                request.Description,
                request.RoleIds,
                request.Status),
            cancellationToken);

        return Ok(group);
    }

    [HttpDelete(ApiRoutes.ById)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roleGroupService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
