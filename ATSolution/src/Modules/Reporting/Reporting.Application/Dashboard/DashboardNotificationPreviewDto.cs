namespace Reporting.Application.Dashboard;

public sealed record DashboardNotificationPreviewDto(
    string Id,
    string Title,
    string Message,
    string Type,
    DateTimeOffset CreatedAt,
    bool IsRead,
    string? ActionUrl = null);
