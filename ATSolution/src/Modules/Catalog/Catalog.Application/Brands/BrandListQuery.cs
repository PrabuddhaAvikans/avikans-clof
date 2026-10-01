using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Brands;

public sealed class BrandListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}
