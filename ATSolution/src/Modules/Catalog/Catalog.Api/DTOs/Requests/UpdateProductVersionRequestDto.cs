using System.Text.Json;
using Catalog.Application.Products;

namespace Catalog.Api.DTOs.Requests;

public sealed record UpdateProductVersionRequestDto
{
    public JsonElement? Specifications { get; init; }
    public JsonElement? Bom { get; init; }
    public JsonElement? Operations { get; init; }
    public IReadOnlyList<ProductAttributeDto>? Attributes { get; init; }
    public JsonElement? CostBreakdown { get; init; }
    public decimal? BasePrice { get; init; }
    public decimal? CostPrice { get; init; }
    public int? LeadTimeDays { get; init; }
    public int? MinOrderQuantity { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? RevisionNotes { get; init; }
    public JsonElement? Images { get; init; }
    public string? Status { get; init; }
}
