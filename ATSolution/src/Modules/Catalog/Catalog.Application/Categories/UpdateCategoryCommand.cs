using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Categories;

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
