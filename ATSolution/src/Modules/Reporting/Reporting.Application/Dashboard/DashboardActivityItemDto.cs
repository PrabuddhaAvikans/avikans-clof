namespace Reporting.Application.Dashboard;

public sealed record DashboardActivityItemDto(
    string Id,
    string Description,
    DateTimeOffset Timestamp,
    string Type,
    string? User = null,
    string? Href = null,
    string? Action = null,
    string? EntityLabel = null,
    string? Severity = null,
    string? EntityId = null,
    IReadOnlyList<DashboardActivityChangeDto>? Changes = null);
