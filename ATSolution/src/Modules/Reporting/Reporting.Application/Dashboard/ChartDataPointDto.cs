namespace Reporting.Application.Dashboard;

public sealed record ChartDataPointDto(string Label, decimal Value, string? Color = null);
