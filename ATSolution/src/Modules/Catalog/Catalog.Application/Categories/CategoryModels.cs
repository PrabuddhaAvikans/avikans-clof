using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Categories;

public sealed class CategoryListQuery : PaginatedRequest
{
    public Guid? ParentId { get; set; }
    public bool? ParentIdIsNull { get; set; }
    public string? Status { get; set; }
}

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentId,
    string? ParentName,
    int SortOrder,
    string Status,
    int ProductCount,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentId,
    int SortOrder,
    string Status,
    string? ImageUrl = null);

public sealed record UpdateCategoryCommand(
    Guid Id,
    string? Name = null,
    string? Slug = null,
    string? Description = null,
    Guid? ParentId = null,
    bool ClearParent = false,
    int? SortOrder = null,
    string? Status = null,
    string? ImageUrl = null);
