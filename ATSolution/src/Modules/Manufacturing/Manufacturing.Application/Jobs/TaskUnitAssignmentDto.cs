using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record TaskUnitAssignmentDto(
    Guid Id,
    Guid TaskUnitId,
    Guid UserId,
    string UserName,
    decimal ContributionPercentage,
    string Status,
    decimal ActualHours,
    decimal OvertimeHours,
    decimal NormalOvertimeHours,
    decimal DoubleOvertimeHours,
    decimal LaborCost,
    decimal RejectedQuantity,
    decimal WasteQuantity,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt);
