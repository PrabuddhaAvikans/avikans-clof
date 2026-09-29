namespace Identity.Api.DTOs.Requests;

public sealed record UpdateRoleRequestDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string>? Permissions { get; init; }
    public string? Status { get; init; }
}
