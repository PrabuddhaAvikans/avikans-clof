using ATSolution.SharedKernel.Constants;

namespace Identity.Api.DTOs.Requests;

public sealed record LoginRequestDto
{
    public string Email { get; init; } = IdentityMessages.AdminEmail;

    public string Password { get; init; } = IdentityMessages.AdminPassword;
}
