namespace Identity.Api.DTOs.Requests;

public sealed record CreateRoleGroupRequestDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public IReadOnlyList<Guid>? RoleIds { get; init; }
    public string Status { get; init; } = "active";
}
