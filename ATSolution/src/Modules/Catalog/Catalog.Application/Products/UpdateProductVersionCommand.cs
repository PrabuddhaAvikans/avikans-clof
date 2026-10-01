using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Products;

public sealed record UpdateProductVersionCommand(
    Guid ProductId,
    Guid VersionId,
    JsonElement? Specifications = null,
    JsonElement? Bom = null,
    JsonElement? Operations = null,
    IReadOnlyList<ProductAttributeDto>? Attributes = null,
    JsonElement? CostBreakdown = null,
    decimal? BasePrice = null,
    decimal? CostPrice = null,
    int? LeadTimeDays = null,
    int? MinOrderQuantity = null,
    IReadOnlyList<string>? Tags = null,
    string? RevisionNotes = null,
    JsonElement? Images = null,
    string? Status = null);
