namespace Identity.Api.DTOs.Requests;

public sealed record UpdateUserRequestDto
{
    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string Email { get; init; } = null!;
}
