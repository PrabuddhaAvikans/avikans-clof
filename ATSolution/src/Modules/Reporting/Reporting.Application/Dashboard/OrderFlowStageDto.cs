namespace Reporting.Application.Dashboard;

public sealed record OrderFlowStageDto(
    string StageKey,
    string Title,
    string SummaryText,
    int Total,
    int AttentionCount,
    string AttentionSeverity,
    IReadOnlyList<OrderFlowStatDto> Stats,
    IReadOnlyList<string> Messages);
