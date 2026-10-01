namespace Configuration.Application.Workflows;

public sealed record WorkflowVersionDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    int VersionNumber,
    string Status,
    bool IsDefault,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<WorkflowStepDefinitionDto> Steps);
