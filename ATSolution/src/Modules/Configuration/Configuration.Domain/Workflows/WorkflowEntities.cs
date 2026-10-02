using ATSolution.Domain.Entities.Common;

namespace Configuration.Domain.Workflows;

public class WorkflowDefinition : Entity<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public string Module { get; private set; } = WorkflowModules.Costing;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static WorkflowDefinition Create(string name, string description, string module, bool isActive)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description,
            Module = module,
            IsActive = isActive,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(string? name, string? description, string? module, bool? isActive)
    {
        if (name is not null) Name = name.Trim();
        if (description is not null) Description = description;
        if (module is not null) Module = module;
        if (isActive.HasValue) IsActive = isActive.Value;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

public class WorkflowVersion : Entity<Guid>, IAuditableEntity
{
    public Guid WorkflowDefinitionId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Status { get; private set; } = WorkflowVersionStatuses.Draft;
    public bool IsDefault { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public string StepsJson { get; private set; } = "[]";
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static WorkflowVersion Create(
        Guid workflowDefinitionId,
        int versionNumber,
        string status,
        bool isDefault,
        string stepsJson,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? effectiveTo = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            VersionNumber = versionNumber,
            Status = status,
            IsDefault = isDefault,
            StepsJson = stepsJson,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Publish()
    {
        Status = WorkflowVersionStatuses.Published;
        PublishedAtUtc ??= DateTimeOffset.UtcNow;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Retire()
    {
        Status = WorkflowVersionStatuses.Retired;
        IsDefault = false;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateSteps(string stepsJson)
    {
        StepsJson = stepsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

public class WorkflowRule : Entity<Guid>, IAuditableEntity
{
    public Guid WorkflowDefinitionId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Priority { get; private set; }
    public bool Enabled { get; private set; } = true;
    public string ConditionsJson { get; private set; } = "[]";
    public Guid WorkflowVersionId { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static WorkflowRule Create(
        Guid workflowDefinitionId,
        string name,
        int priority,
        bool enabled,
        string conditionsJson,
        Guid workflowVersionId)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowRule
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            Name = name.Trim(),
            Priority = priority,
            Enabled = enabled,
            ConditionsJson = conditionsJson,
            WorkflowVersionId = workflowVersionId,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string? name,
        int? priority,
        bool? enabled,
        string? conditionsJson,
        Guid? workflowVersionId)
    {
        if (name is not null) Name = name.Trim();
        if (priority.HasValue) Priority = priority.Value;
        if (enabled.HasValue) Enabled = enabled.Value;
        if (conditionsJson is not null) ConditionsJson = conditionsJson;
        if (workflowVersionId.HasValue) WorkflowVersionId = workflowVersionId.Value;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

public class WorkflowInstance : Entity<Guid>, IAuditableEntity
{
    public string SubjectType { get; private set; } = WorkflowSubjectTypes.CostingRequest;
    public string SubjectId { get; private set; } = null!;
    public Guid WorkflowDefinitionId { get; private set; }
    public string WorkflowDefinitionName { get; private set; } = null!;
    public Guid WorkflowVersionId { get; private set; }
    public int WorkflowVersionNumber { get; private set; }
    public string Status { get; private set; } = WorkflowInstanceStatuses.NotStarted;
    public int CurrentStepOrder { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string StepsJson { get; private set; } = "[]";
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static WorkflowInstance Create(
        string subjectType,
        string subjectId,
        Guid workflowDefinitionId,
        string workflowDefinitionName,
        Guid workflowVersionId,
        int workflowVersionNumber,
        string status,
        int currentStepOrder,
        string stepsJson,
        DateTimeOffset? startedAtUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            SubjectType = subjectType,
            SubjectId = subjectId,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowDefinitionName = workflowDefinitionName,
            WorkflowVersionId = workflowVersionId,
            WorkflowVersionNumber = workflowVersionNumber,
            Status = status,
            CurrentStepOrder = currentStepOrder,
            StartedAtUtc = startedAtUtc ?? now,
            StepsJson = stepsJson,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateStatus(string status, int? currentStepOrder = null, string? stepsJson = null)
    {
        Status = status;
        if (currentStepOrder.HasValue) CurrentStepOrder = currentStepOrder.Value;
        if (stepsJson is not null) StepsJson = stepsJson;
        if (status is WorkflowInstanceStatuses.Approved
            or WorkflowInstanceStatuses.Rejected
            or WorkflowInstanceStatuses.Cancelled)
        {
            CompletedAtUtc ??= DateTimeOffset.UtcNow;
        }
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

public static class WorkflowModules
{
    public const string Costing = "costing";
    public const string Sales = "sales";
}

public static class WorkflowStageKeys
{
    public const string Quotation = "quotation";
    public const string SalesOrder = "sales_order";
    public const string Estimation = "estimation";
    public const string Costing = "costing";
    public const string Production = "production";
    public const string Delivery = "delivery";
    public const string Completed = "completed";
}

public static class WorkflowVersionStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Retired = "retired";
}

public static class WorkflowInstanceStatuses
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
    public const string ChangesRequested = "changes_requested";
}

public static class WorkflowSubjectTypes
{
    public const string CostingRequest = "costing_request";
}
