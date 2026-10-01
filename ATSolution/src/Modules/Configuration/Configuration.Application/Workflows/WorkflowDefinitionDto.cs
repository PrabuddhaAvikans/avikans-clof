namespace Configuration.Application.Workflows;

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    string Description,
    string Module,
    bool IsActive);
