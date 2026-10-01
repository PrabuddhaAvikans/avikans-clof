using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record QualityInspectionChecklistItemDto(
    Guid Id,
    string Name,
    bool? Passed,
    string? Notes);
