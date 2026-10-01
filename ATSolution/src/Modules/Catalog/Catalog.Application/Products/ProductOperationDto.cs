using System.Text.Json;
namespace Catalog.Application.Products;

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
