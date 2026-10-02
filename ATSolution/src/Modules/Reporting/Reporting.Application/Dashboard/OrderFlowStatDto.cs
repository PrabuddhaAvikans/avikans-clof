namespace Reporting.Application.Dashboard;

public sealed record OrderFlowStatDto(
    string Key,
    string Label,
    int Count,
    string Severity);
