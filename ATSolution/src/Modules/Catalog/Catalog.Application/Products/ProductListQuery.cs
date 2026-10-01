using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Products;

public sealed class ProductListQuery : PaginatedRequest
{
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string? Status { get; set; }
    public string? VersionStatus { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? AvailableForCustomerId { get; set; }
    public string? Tags { get; set; }
}
