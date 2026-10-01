using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record ManufacturingReworkDto(
    Guid Id,
    string ReworkNumber,
    Guid OriginalTaskId,
    Guid ReworkTaskId,
    string Reason,
    decimal Quantity,
    decimal? AdditionalTimeHours,
    IReadOnlyList<TaskMaterialUsageDto>? AdditionalMaterials,
    decimal? AdditionalCost,
    string? Result,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Notes);
