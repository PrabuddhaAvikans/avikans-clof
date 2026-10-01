using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Products;

public sealed record UpdateProductCommand(
    Guid Id,
    string? Sku = null,
    string? Name = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string? ProductType = null,
    string? Status = null,
    decimal? BasePrice = null,
    decimal? CostPrice = null,
    int? LeadTimeDays = null,
    int? MinOrderQuantity = null,
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
