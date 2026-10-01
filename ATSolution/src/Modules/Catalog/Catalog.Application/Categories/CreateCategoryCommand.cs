using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Categories;

public sealed record CreateCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentId,
    int SortOrder,
    string Status,
    string? ImageUrl = null);
