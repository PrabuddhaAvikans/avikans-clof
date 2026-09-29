namespace Identity.Api.DTOs.Requests;

public sealed record CreateRoleRequestDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public IReadOnlyList<string>? Permissions { get; init; }
    public string Status { get; init; } = "active";
}
