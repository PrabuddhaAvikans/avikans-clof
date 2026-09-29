using ATSolution.SharedKernel.Constants;
using Identity.Api.DTOs.Requests;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Route(ApiRoutes.Auth.Base)]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Login)]
    public async Task<ActionResult<LoginResult>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(
                new LoginCommand(request.Email, request.Password),
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = IdentityMessages.InvalidCredentials });
        }
    }
}
