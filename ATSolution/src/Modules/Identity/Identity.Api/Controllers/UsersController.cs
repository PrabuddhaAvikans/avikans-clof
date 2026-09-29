using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Identity.Api.DTOs.Requests;
using Identity.Application.Abstractions;
using Identity.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Identity.Users)]
[ApiController]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserDetailDto>>> List(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.ListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> Create(
        [FromBody] CreateManagedUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.CreateAsync(
            new CreateManagedUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.RoleId,
                request.RoleGroupIds ?? Array.Empty<Guid>(),
                request.Status,
                request.Phone,
                request.Department,
                request.JobTitle,
                request.Password),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> Update(
        Guid id,
        [FromBody] UpdateManagedUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.UpdateAsync(
            new UpdateManagedUserCommand(
                id,
                request.Email,
                request.FirstName,
                request.LastName,
                request.RoleId,
                request.RoleGroupIds,
                request.Status,
                request.Phone,
                request.Department,
                request.JobTitle),
            cancellationToken);

        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userManagementService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded(IdentityMessages.UserDeletedSuccessfully));
    }

    [HttpGet("{id:guid}/permission-assignment")]
    public async Task<ActionResult<PermissionAssignmentDto>> GetPermissionAssignment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await _userManagementService.GetPermissionAssignmentAsync(id, cancellationToken);
        return Ok(assignment);
    }
}
