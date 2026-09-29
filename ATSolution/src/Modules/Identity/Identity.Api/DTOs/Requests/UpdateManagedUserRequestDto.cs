namespace Identity.Api.DTOs.Requests;

public sealed record UpdateManagedUserRequestDto
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public Guid? RoleId { get; init; }
    public IReadOnlyList<Guid>? RoleGroupIds { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public string? Status { get; init; }
}
