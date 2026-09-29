namespace Catalog.Api.DTOs.Requests;

public sealed record UpdateCategoryRequestDto
{
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public bool ClearParent { get; init; }
    public int? SortOrder { get; init; }
    public string? Status { get; init; }
    public string? ImageUrl { get; init; }
}
