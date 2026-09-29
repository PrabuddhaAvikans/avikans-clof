using ATSolution.Domain.Entities.Common;
using Sales.Domain.Common;

namespace Sales.Domain.Costing;

public class CostingRequest : Entity<Guid>, IAuditableEntity
{
    public string Number { get; private set; } = null!;
    public Guid SalesOrderId { get; private set; }
    public string SalesOrderNumber { get; private set; } = null!;
    public Guid? QuotationId { get; private set; }
    public string? QuotationNumber { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string ProjectName { get; private set; } = null!;
    public string RequestType { get; private set; } = SalesDefaults.CostingRequestType;
    public DateTimeOffset RequestedDateUtc { get; private set; }
    public decimal TotalEstimate { get; private set; }
    public decimal ProposedPrice { get; private set; }
    public decimal MarginPercent { get; private set; }
    public decimal TargetMargin { get; private set; } = 25;
    public string RiskFlag { get; private set; } = PriorityValues.Medium;
    public string SlaRemaining { get; private set; } = SalesDefaults.DefaultSlaRemaining;
    public string Status { get; private set; } = CostingRequestStatuses.Pending;
    public string CoatingStatus { get; private set; } = CoatingStatuses.Pending;
    public string Currency { get; private set; } = SalesDefaults.Currency;
    public string PaymentTerms { get; private set; } = SalesDefaults.PaymentTerms;
    public string LineItemsJson { get; private set; } = "[]";
    public string CoatingItemsJson { get; private set; } = "[]";
    public string EstimationMaterialsJson { get; private set; } = "[]";
    public string EstimationProductLinesJson { get; private set; } = "[]";
    public string AttachmentsJson { get; private set; } = "[]";
    public string Notes { get; private set; } = string.Empty;
    public string RequesterJson { get; private set; } = "{}";
    public string ApprovalLevelsJson { get; private set; } = "[]";
    public string HistoryJson { get; private set; } = "[]";
    public string CommentsJson { get; private set; } = "[]";
    public string? ConfigSnapshotJson { get; private set; }
    public string? WorkflowDefinitionId { get; private set; }
    public string? WorkflowVersionId { get; private set; }
    public string? WorkflowInstanceId { get; private set; }
    public int? WorkflowVersionNumber { get; private set; }
    public string? WorkflowName { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static CostingRequest Create(
        string number,
        Guid salesOrderId,
        string salesOrderNumber,
        Guid? quotationId,
        string? quotationNumber,
        string customerName,
        string projectName,
        decimal totalEstimate,
        decimal proposedPrice,
        decimal marginPercent,
        string currency,
        string paymentTerms,
        string lineItemsJson,
        string coatingItemsJson,
        string estimationMaterialsJson,
        string estimationProductLinesJson,
        string requesterJson,
        string approvalLevelsJson,
        string historyJson,
        string status,
        string coatingStatus,
        string? configSnapshotJson,
        string? notes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new CostingRequest
        {
            Id = Guid.NewGuid(),
            Number = number,
            SalesOrderId = salesOrderId,
            SalesOrderNumber = salesOrderNumber,
            QuotationId = quotationId,
            QuotationNumber = quotationNumber,
            CustomerName = customerName,
            ProjectName = projectName,
            RequestedDateUtc = now,
            TotalEstimate = totalEstimate,
            ProposedPrice = proposedPrice,
            MarginPercent = marginPercent,
            Currency = currency,
            PaymentTerms = paymentTerms,
            LineItemsJson = lineItemsJson,
            CoatingItemsJson = coatingItemsJson,
            EstimationMaterialsJson = estimationMaterialsJson,
            EstimationProductLinesJson = estimationProductLinesJson,
            RequesterJson = requesterJson,
            ApprovalLevelsJson = approvalLevelsJson,
            HistoryJson = historyJson,
            Status = status,
            CoatingStatus = coatingStatus,
            ConfigSnapshotJson = configSnapshotJson,
            Notes = notes ?? string.Empty,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void SyncContent(
        decimal totalEstimate,
        decimal proposedPrice,
        decimal marginPercent,
        string lineItemsJson,
        string coatingItemsJson,
        string estimationMaterialsJson,
        string estimationProductLinesJson,
        string status,
        string coatingStatus,
        string? historyJson = null)
    {
        TotalEstimate = totalEstimate;
        ProposedPrice = proposedPrice;
        MarginPercent = marginPercent;
        LineItemsJson = lineItemsJson;
        CoatingItemsJson = coatingItemsJson;
        EstimationMaterialsJson = estimationMaterialsJson;
        EstimationProductLinesJson = estimationProductLinesJson;
        Status = status;
        CoatingStatus = coatingStatus;
        if (historyJson is not null) HistoryJson = historyJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SubmitCoating(
        string coatingItemsJson,
        string? estimationMaterialsJson,
        string? lineItemsJson,
        decimal totalEstimate,
        decimal marginPercent,
        string status,
        string historyJson,
        string? approvalLevelsJson)
    {
        CoatingItemsJson = coatingItemsJson;
        if (estimationMaterialsJson is not null) EstimationMaterialsJson = estimationMaterialsJson;
        if (lineItemsJson is not null) LineItemsJson = lineItemsJson;
        TotalEstimate = totalEstimate;
        MarginPercent = marginPercent;
        CoatingStatus = CoatingStatuses.Submitted;
        Status = status;
        HistoryJson = historyJson;
        if (approvalLevelsJson is not null) ApprovalLevelsJson = approvalLevelsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetStatus(string status, string historyJson, string? approvalLevelsJson = null)
    {
        Status = status;
        HistoryJson = historyJson;
        if (approvalLevelsJson is not null) ApprovalLevelsJson = approvalLevelsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetComments(string commentsJson)
    {
        CommentsJson = commentsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;
}
