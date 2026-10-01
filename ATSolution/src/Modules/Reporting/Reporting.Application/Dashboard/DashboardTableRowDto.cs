namespace Reporting.Application.Dashboard;

public sealed record DashboardTableRowDto(
    string Id,
    string Reference,
    string Title,
    string Status,
    decimal? Amount,
    string Date,
    string? Customer = null,
    string? Priority = null,
    string? Href = null);
