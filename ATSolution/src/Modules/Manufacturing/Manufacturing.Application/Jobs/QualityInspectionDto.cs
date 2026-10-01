using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record QualityInspectionDto(
    Guid Id,
    string InspectionNumber,
    Guid InspectorId,
    string InspectorName,
    string Status,
    IReadOnlyList<QualityInspectionChecklistItemDto> ChecklistItems,
    DateTimeOffset? InspectedAt,
    string? Notes);
