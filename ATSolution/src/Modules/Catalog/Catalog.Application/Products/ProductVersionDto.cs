using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record ProductVersionDto(
    Guid Id,
    Guid ProductId,
    int VersionNumber,
    string Label,
    string Status,
    bool IsLocked,
    ProductSpecificationsDto Specifications,
    IReadOnlyList<BomItemDto> Bom,
    IReadOnlyList<ProductOperationDto> Operations,
    IReadOnlyList<ProductAttributeDto> Attributes,
    IReadOnlyList<ProductImageDto> Images,
    CostBreakdownDto CostBreakdown,
    decimal BasePrice,
    decimal CostPrice,
    int LeadTimeDays,
    int MinOrderQuantity,
    IReadOnlyList<string> Tags,
    string? RevisionNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReleasedAt,
    string? ReleasedBy,
    DateTimeOffset? ApprovedAt);
