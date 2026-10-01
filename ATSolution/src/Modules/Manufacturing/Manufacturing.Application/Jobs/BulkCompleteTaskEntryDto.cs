using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record BulkCompleteTaskEntryDto(
    Guid TaskId,
    decimal? CompletedQuantity,
    decimal? RejectedQuantity,
    decimal? WasteQuantity,
    IReadOnlyList<TaskContributorInputDto>? Contributors,
    decimal? ActualHours,
    decimal? NormalOvertimeHours,
    decimal? DoubleOvertimeHours,
    string? Notes);
