namespace Identity.Api.DTOs.Requests;

public sealed record CreateManagedUserRequestDto
{
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? Phone { get; init; }
    public Guid RoleId { get; init; }
    public IReadOnlyList<Guid>? RoleGroupIds { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public string Status { get; init; } = "active";
    public string? Password { get; init; }
}
