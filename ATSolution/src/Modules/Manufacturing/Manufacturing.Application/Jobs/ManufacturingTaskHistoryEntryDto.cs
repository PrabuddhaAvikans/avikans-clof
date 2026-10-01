using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record ManufacturingTaskHistoryEntryDto(
    Guid Id,
    Guid TaskId,
    Guid UserId,
    string UserName,
    DateTimeOffset OccurredAt,
    string Action,
    string? OldStatus,
    string? NewStatus,
    string? Comments);
