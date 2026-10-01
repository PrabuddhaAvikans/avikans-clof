using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Products;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? BrandId,
    string ProductType,
    string Status,
    decimal BasePrice,
    decimal CostPrice,
    int LeadTimeDays = 0,
    int MinOrderQuantity = 1,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<ProductAttributeDto>? Attributes = null,
    Guid? CustomerId = null,
    Guid? ProjectId = null,
    string? ProjectName = null,
    decimal? WeightKg = null,
    string? Dimensions = null,
    JsonElement? Specifications = null,
    JsonElement? Bom = null,
    JsonElement? Operations = null,
    JsonElement? CostBreakdown = null,
    string? RevisionNotes = null,
    JsonElement? Images = null);
