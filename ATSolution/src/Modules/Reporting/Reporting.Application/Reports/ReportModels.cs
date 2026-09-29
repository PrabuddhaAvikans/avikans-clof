namespace Reporting.Application.Reports;

public sealed record ReportColumnDto(
    string Key,
    string Label,
    string Type,
    string? Align = null);

public sealed record ReportKpiDto(
    string Id,
    string Label,
    decimal Value,
    string Type,
    string? Description = null);

public sealed record ReportDatasetDto(
    string ReportId,
    string Title,
    string GeneratedAt,
    IReadOnlyList<ReportKpiDto> Kpis,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    IReadOnlyList<ReportColumnDto> Columns);
