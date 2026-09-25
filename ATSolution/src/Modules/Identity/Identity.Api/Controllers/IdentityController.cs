using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using AutoMapper;
using Identity.Api.DTOs.Requests;
using Identity.Api.DTOs.Responses;
using Identity.Application.Abstractions;
using Identity.Application.Users;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Route(ApiRoutes.Identity.Base)]
[ApiController]
public sealed class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IMapper _mapper;

    public IdentityController(IIdentityService identityService, IMapper mapper)
    {
        _identityService = identityService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _identityService.GetUsersAsync(cancellationToken);

        return Ok(_mapper.Map<IReadOnlyList<UserResponseDto>>(users));
    }

    [HttpGet(ApiRoutes.Identity.EmailRoute)]
    public async Task<ActionResult<UserResponseDto>> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByEmailAsync(email, cancellationToken);

        return user is null ? NotFound() : Ok(_mapper.Map<UserResponseDto>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(
        CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _identityService.CreateUserAsync(
            _mapper.Map<CreateUserCommand>(request),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetUserByEmail),
            new { email = user.Email },
            _mapper.Map<UserResponseDto>(user));
    }

    [HttpPut(ApiRoutes.Identity.EmailRoute)]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(
        string email,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateUserCommand>(request) with
        {
            CurrentEmail = email
        };

        var user = await _identityService.UpdateUserAsync(command, cancellationToken);

        return Ok(_mapper.Map<UserResponseDto>(user));
    }

    [HttpPatch(ApiRoutes.Identity.EmailRoute)]
    public async Task<ActionResult<UserResponseDto>> PatchUser(
        string email,
        PatchUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<PatchUserCommand>(request) with
        {
            CurrentEmail = email
        };

        var user = await _identityService.PatchUserAsync(command, cancellationToken);

        return Ok(_mapper.Map<UserResponseDto>(user));
    }

    [HttpDelete(ApiRoutes.Identity.EmailRoute)]
    public async Task<ActionResult<ApiResponse>> DeleteUser(string email, CancellationToken cancellationToken)
    {
        await _identityService.DeleteUserAsync(new DeleteUserCommand(email), cancellationToken);

        return Ok(ApiResponse.Succeeded(IdentityMessages.UserDeletedSuccessfully));
    }
}
