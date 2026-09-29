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

public sealed record ProductImageDto(Guid Id, string Url, string? Alt, bool IsPrimary, int SortOrder);

public sealed record BomAlternativeDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    string Unit,
    decimal UnitCost,
    bool IsApproved,
    string? Notes);

public sealed record BomItemDto(
    Guid Id,
    int Sequence,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    decimal Quantity,
    string Unit,
    decimal WastePercent,
    decimal RequiredQuantity,
    decimal UnitCost,
    decimal LineCost,
    bool IsRequired,
    string? Notes,
    IReadOnlyList<BomAlternativeDto> Alternatives);

public sealed record ProductOperationDto(
    Guid Id,
    string Name,
    int Sequence,
    string? Description,
    string Workstation,
    decimal EstimatedHours,
    decimal? LabourCostRate,
    string? MachineName,
    decimal? MachineCost,
    bool IsRequired,
    bool IsEnabled,
    string? Notes,
    IReadOnlyList<Guid>? PrerequisiteOperationIds,
    bool? IsQualityCheck);

public sealed record ProductAttributeDto(Guid Id, string Name, string Value, string? Unit);

public sealed record ProductAccessoryDto(Guid Id, string Handle, string Name, string? Sku, decimal? Quantity);

public sealed record ProductSpecificationsDto
{
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Size { get; init; }
    public string? Shape { get; init; }
    public string? Design { get; init; }
    public string? Finish { get; init; }
    public string? Colour { get; init; }
    public string? Glass { get; init; }
    public string? Wiring { get; init; }
    public string? MountingType { get; init; }
    public string? MountingBracket { get; init; }
    public string? Voltage { get; init; }
    public decimal? Wattage { get; init; }
    public string? LedType { get; init; }
    public string? ColorTemperature { get; init; }
    public string? Driver { get; init; }
    public decimal? LengthMm { get; init; }
    public decimal? WidthMm { get; init; }
    public decimal? HeightMm { get; init; }
    public decimal? DiameterMm { get; init; }
    public decimal? LumenOutput { get; init; }
    public decimal? Efficacy { get; init; }
    public string? Cri { get; init; }
    public string? BeamAngle { get; init; }
    public string? IpRating { get; init; }
    public string? InputVoltage { get; init; }
    public decimal? PowerFactor { get; init; }
    public string? Dimming { get; init; }
    public decimal? OpTempMin { get; init; }
    public decimal? OpTempMax { get; init; }
    public decimal? InputPower { get; init; }
    public decimal? InputCurrent { get; init; }
    public string? DriverType { get; init; }
    public string? DriverBrand { get; init; }
    public string? CoatingFinish { get; init; }
    public string? CoatingProcess { get; init; }
    public string? MaterialPrimary { get; init; }
    public string? MaterialSecondary { get; init; }
    public string? Certifications { get; init; }
    public string? Warranty { get; init; }
    public string? ManufacturingNotes { get; init; }
    public IReadOnlyList<ProductAccessoryDto>? Accessories { get; init; }
}

public sealed record CostSheetLineDto(Guid Id, string Handle, decimal Amount);

public sealed record CostBreakdownDto(
    decimal MaterialCost,
    decimal LabourCost,
    decimal CoatingFinishingCost,
    decimal MachineCost,
    decimal OverheadCost,
    decimal OtherCost,
    IReadOnlyList<CostSheetLineDto>? ExtraLines = null,
    string? ConfigurationId = null,
    string? Notes = null);

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

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    Guid BrandId,
    string BrandName,
    string ProductType,
    Guid? CustomerId,
    string? CustomerName,
    Guid? ProjectId,
    string? ProjectName,
    string Currency,
    string Status,
    Guid CurrentVersionId,
    IReadOnlyList<ProductVersionDto> Versions,
    decimal BasePrice,
    decimal CostPrice,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<BomItemDto> Bom,
    IReadOnlyList<ProductOperationDto> Operations,
    IReadOnlyList<ProductAttributeDto> Attributes,
    decimal? WeightKg,
    string? Dimensions,
    int LeadTimeDays,
    int MinOrderQuantity,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy);

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

public sealed record UpdateProductHeaderCommand(
    Guid Id,
    string? Sku = null,
    string? Name = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string? ProductType = null,
    Guid? CustomerId = null,
    Guid? ProjectId = null,
    string? ProjectName = null,
    string? Status = null);

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

public sealed record ReviseProductVersionCommand(
    Guid ProductId,
    Guid SourceVersionId,
    string? RevisionNotes = null);
