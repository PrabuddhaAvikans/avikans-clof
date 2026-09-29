using ATSolution.Domain.Entities.Common;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Tasks;

namespace Manufacturing.Domain.Jobs;

public class ManufacturingJob : Entity<Guid>, IAuditableEntity
{
    public string Number { get; private set; } = null!;
    public Guid SalesOrderId { get; private set; }
    public string SalesOrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Guid? ProductVersionId { get; private set; }
    public string ProductVersionLabel { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Status { get; private set; } = ManufacturingJobStatuses.Draft;
    public string Priority { get; private set; } = PriorityValues.Medium;
    public DateTimeOffset PlannedStart { get; private set; }
    public DateTimeOffset PlannedEnd { get; private set; }
    public DateTimeOffset? ActualStartUtc { get; private set; }
    public DateTimeOffset? ActualEndUtc { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string? AssignedToName { get; private set; }
    public string? Notes { get; private set; }
    public decimal OverallProgress { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public decimal ActualCost { get; private set; }
    public string MaterialRequirementsJson { get; private set; } = "[]";
    public string? QualityInspectionJson { get; private set; }
    public string? CompletionOutcomeJson { get; private set; }
    public string ReworksJson { get; private set; } = "[]";
    public string CreatedBy { get; private set; } = "system";
    public string CreatedByName { get; private set; } = "System";
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<ManufacturingTask> Tasks { get; private set; } = new List<ManufacturingTask>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static ManufacturingJob Create(
        string number,
        Guid salesOrderId,
        string salesOrderNumber,
        Guid customerId,
        string customerName,
        Guid productId,
        string productSku,
        string productName,
        Guid? productVersionId,
        string productVersionLabel,
        decimal quantity,
        string priority,
        DateTimeOffset plannedStart,
        DateTimeOffset plannedEnd,
        Guid? assignedToUserId,
        string? assignedToName,
        string? notes,
        string materialRequirementsJson,
        string createdBy,
        string createdByName)
    {
        var now = DateTimeOffset.UtcNow;
        return new ManufacturingJob
        {
            Id = Guid.NewGuid(),
            Number = number,
            SalesOrderId = salesOrderId,
            SalesOrderNumber = salesOrderNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            ProductId = productId,
            ProductSku = productSku,
            ProductName = productName,
            ProductVersionId = productVersionId,
            ProductVersionLabel = productVersionLabel,
            Quantity = quantity,
            Status = ManufacturingJobStatuses.Draft,
            Priority = priority,
            PlannedStart = plannedStart,
            PlannedEnd = plannedEnd,
            AssignedToUserId = assignedToUserId,
            AssignedToName = assignedToName,
            Notes = notes,
            MaterialRequirementsJson = materialRequirementsJson,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string? priority,
        DateTimeOffset? plannedStart,
        DateTimeOffset? plannedEnd,
        Guid? assignedToUserId,
        string? assignedToName,
        string? notes,
        string? status)
    {
        if (priority is not null) Priority = priority;
        if (plannedStart.HasValue) PlannedStart = plannedStart.Value;
        if (plannedEnd.HasValue) PlannedEnd = plannedEnd.Value;
        if (assignedToUserId.HasValue) AssignedToUserId = assignedToUserId;
        if (assignedToName is not null) AssignedToName = assignedToName;
        if (notes is not null) Notes = notes;
        if (status is not null) Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetStatus(string status)
    {
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetMaterialRequirementsJson(string json)
    {
        MaterialRequirementsJson = json;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetQualityInspectionJson(string? json)
    {
        QualityInspectionJson = json;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetCompletionOutcomeJson(string? json)
    {
        CompletionOutcomeJson = json;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetReworksJson(string json)
    {
        ReworksJson = json;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkStarted(DateTimeOffset now)
    {
        ActualStartUtc ??= now;
        if (Status is ManufacturingJobStatuses.Draft
            or ManufacturingJobStatuses.Planned
            or ManufacturingJobStatuses.ReadyToStart
            or ManufacturingJobStatuses.MaterialsPending)
        {
            Status = ManufacturingJobStatuses.InProgress;
        }
        ModifiedOnUtc = now;
    }

    public void MarkCompleted(DateTimeOffset now)
    {
        Status = ManufacturingJobStatuses.Completed;
        ActualEndUtc = now;
        OverallProgress = 100;
        ModifiedOnUtc = now;
    }

    public void SetOverallProgress(decimal progress)
    {
        OverallProgress = Math.Clamp(progress, 0, 100);
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetCosts(decimal estimated, decimal actual)
    {
        EstimatedCost = estimated;
        ActualCost = actual;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;
}
