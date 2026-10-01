using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record BulkCompleteTasksInputDto(
    IReadOnlyList<BulkCompleteTaskEntryDto>? Tasks,
    IReadOnlyList<Guid>? TaskIds,
    string? Notes);
