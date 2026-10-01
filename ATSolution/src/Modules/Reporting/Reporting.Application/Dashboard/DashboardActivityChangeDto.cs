namespace Reporting.Application.Dashboard;

public sealed record DashboardActivityChangeDto(string Field, string? From = null, string? To = null);
