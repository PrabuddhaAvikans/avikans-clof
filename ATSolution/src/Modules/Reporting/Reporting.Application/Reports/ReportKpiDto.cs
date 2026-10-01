namespace Reporting.Application.Reports;

public sealed record ReportKpiDto(
    string Id,
    string Label,
    decimal Value,
    string Type,
    string? Description = null);
