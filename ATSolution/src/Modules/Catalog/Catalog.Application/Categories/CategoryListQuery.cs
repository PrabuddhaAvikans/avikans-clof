using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Categories;

public sealed class CategoryListQuery : PaginatedRequest
{
    public Guid? ParentId { get; set; }
    public bool? ParentIdIsNull { get; set; }
    public string? Status { get; set; }
}
