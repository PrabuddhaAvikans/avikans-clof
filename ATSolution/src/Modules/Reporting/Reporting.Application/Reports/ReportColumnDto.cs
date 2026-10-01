namespace Reporting.Application.Reports;

public sealed record ReportColumnDto(
    string Key,
    string Label,
    string Type,
    string? Align = null);
