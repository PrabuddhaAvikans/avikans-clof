using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record TaskUnitDto(
    Guid Id,
    Guid TaskId,
    int UnitNo,
    decimal ProgressPercentage,
    string Status,
    IReadOnlyList<TaskUnitAssignmentDto> Assignments);
