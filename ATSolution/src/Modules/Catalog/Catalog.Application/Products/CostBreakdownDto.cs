using System.Text.Json;
namespace Catalog.Application.Products;

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
