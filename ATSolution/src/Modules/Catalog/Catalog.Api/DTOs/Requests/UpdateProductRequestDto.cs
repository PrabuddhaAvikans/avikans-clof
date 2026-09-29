using System.Text.Json;
using Catalog.Application.Products;

namespace Catalog.Api.DTOs.Requests;

public sealed record UpdateProductRequestDto
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public string? ProductType { get; init; }
    public string? Status { get; init; }
    public decimal? BasePrice { get; init; }
    public decimal? CostPrice { get; init; }
    public int? LeadTimeDays { get; init; }
    public int? MinOrderQuantity { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyList<ProductAttributeDto>? Attributes { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public JsonElement? Specifications { get; init; }
    public JsonElement? Bom { get; init; }
    public JsonElement? Operations { get; init; }
    public JsonElement? CostBreakdown { get; init; }
    public string? RevisionNotes { get; init; }
    public JsonElement? Images { get; init; }
}
