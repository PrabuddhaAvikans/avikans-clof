namespace Reporting.Application.Reports;

public sealed record ReportDatasetDto(
    string ReportId,
    string Title,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReportKpiDto> Kpis,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    IReadOnlyList<ReportColumnDto> Columns);
