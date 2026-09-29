using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Costing;

public sealed class CostingListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public string? CoatingStatus { get; set; }
    public Guid? SalesOrderId { get; set; }
    public bool? LinkedToSalesOrder { get; set; }
}

public sealed record CostingRequesterDto(
    string Name,
    string Title,
    string Email,
    string AvatarInitials);

public sealed record ApprovalLevelDto(
    Guid Id,
    string Role,
    string AssigneeName,
    string Status);

public sealed record ApprovalHistoryEntryDto(
    Guid Id,
    string Action,
    string UserName,
    DateTimeOffset Timestamp,
    string? Comment);

public sealed record CostingCommentDto(
    Guid Id,
    string Comment,
    string UserName,
    DateTimeOffset Timestamp);

public sealed record CostingRequestDto(
    Guid Id,
    string RequestNumber,
    string CustomerName,
    string ProjectName,
    string RequestType,
    DateTimeOffset RequestedDate,
    decimal TotalEstimate,
    decimal ProposedPrice,
    decimal MarginPercent,
    decimal TargetMargin,
    string RiskFlag,
    string SlaRemaining,
    string Status,
    string CoatingStatus,
    string Currency,
    string PaymentTerms,
    JsonElement LineItems,
    JsonElement CoatingItems,
    JsonElement EstimationMaterials,
    JsonElement EstimationProductLines,
    JsonElement Attachments,
    string Notes,
    CostingRequesterDto Requester,
    IReadOnlyList<ApprovalLevelDto> ApprovalLevels,
    IReadOnlyList<ApprovalHistoryEntryDto> History,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    Guid? QuotationId,
    string? QuotationNumber,
    string? WorkflowDefinitionId,
    string? WorkflowVersionId,
    string? WorkflowInstanceId,
    int? WorkflowVersionNumber,
    string? WorkflowName);

public sealed record CoatingSubmitItemDto(
    Guid? Id,
    Guid? ProductId,
    string ProductName,
    string Finish,
    string Process,
    decimal Quantity,
    decimal UnitCost,
    Guid? SalesOrderLineItemId = null,
    string? SourceType = null,
    string? ProductVersionLabel = null,
    string? ProductSku = null);

public sealed record EstimationMaterialInputDto(
    Guid? Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    decimal Quantity,
    string Unit,
    decimal WastePercent,
    decimal UnitCost,
    bool IsRequired,
    Guid? AlternativeItemId,
    string? AlternativeItemName,
    string? Notes,
    Guid? SalesOrderLineItemId,
    string? SourceType,
    string? SourceProductName,
    string? ProductVersionLabel);

public sealed record SubmitCoatingCommand(
    Guid Id,
    IReadOnlyList<CoatingSubmitItemDto> Items,
    IReadOnlyList<EstimationMaterialInputDto>? Materials,
    string? Notes,
    string? ActorName);

public sealed record CostingCommentCommand(Guid Id, string Comment, string? ActorName);
public sealed record CostingDecisionCommand(Guid Id, string? Comment, string? ActorName);
public sealed record UpdateCostingNotesCommand(Guid Id, string Notes);
