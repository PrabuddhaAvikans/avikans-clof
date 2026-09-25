namespace Identity.Api.DTOs.Requests;

public sealed record PatchUserRequestDto
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }
}
