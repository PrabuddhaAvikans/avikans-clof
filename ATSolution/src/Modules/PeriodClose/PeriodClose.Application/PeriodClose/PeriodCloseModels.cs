using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

public sealed class BusinessPeriodListQuery : PaginatedRequest
{
    public string? BranchId { get; set; }
    public string? Status { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}

public sealed class MonthlyPeriodListQuery : PaginatedRequest
{
    public string? BranchId { get; set; }
    public string? Status { get; set; }
    public int? Year { get; set; }
}

public sealed record PeriodActorDto(string UserId, string UserName);

public sealed record BusinessPeriodDto(
    Guid Id,
    string BranchId,
    string BusinessDate,
    string Status,
    DateTimeOffset OpenedAt,
    string OpenedBy,
    string OpenedByName,
    DateTimeOffset? ClosedAt,
    string? ClosedBy,
    string? ClosedByName,
    DateTimeOffset? ReopenedAt,
    string? ReopenedBy,
    string? ReopenedByName,
    string? ReopenReason,
    DateTimeOffset? OriginalClosedAt,
    string? OriginalClosedBy,
    string? OriginalClosedByName,
    int CloseCount);

public sealed record MonthlyPeriodDto(
    Guid Id,
    string BranchId,
    int Year,
    int Month,
    string Status,
    DateTimeOffset StartedAt,
    string StartedBy,
    string StartedByName,
    DateTimeOffset? ClosedAt,
    string? ClosedBy,
    string? ClosedByName,
    DateTimeOffset? ReopenedAt,
    string? ReopenedBy,
    string? ReopenedByName,
    string? ReopenReason,
    DateTimeOffset? OriginalClosedAt,
    string? OriginalClosedBy,
    string? OriginalClosedByName,
    int CloseCount);

public sealed record DayCloseValidationIssueDto(
    Guid Id,
    Guid BusinessPeriodId,
    string ValidationCode,
    string ValidationType,
    string Message,
    string? EntityType,
    string? EntityId,
    bool IsBlocking);

public sealed record MonthlyCloseValidationIssueDto(
    Guid Id,
    Guid MonthlyPeriodId,
    string ValidationCode,
    string ValidationType,
    string Message,
    string? EntityType,
    string? EntityId,
    bool IsBlocking);

public sealed record DailyTransactionRefsDto(
    IReadOnlyList<string> OrderIds,
    IReadOnlyList<string> InvoiceIds,
    IReadOnlyList<string> PaymentIds,
    IReadOnlyList<string> DeliveryIds,
    IReadOnlyList<string> StockMovementIds,
    IReadOnlyList<string> ProductionJobIds);

public sealed record DailyClosingSummaryDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string BranchId,
    int OrdersCreated,
    int ProductionJobs,
    decimal CompletedProductionQty,
    decimal PartialProductionQty,
    int Invoices,
    decimal InvoiceTotal,
    int Payments,
    decimal PaymentTotal,
    int Deliveries,
    int MaterialIssues,
    int MaterialReturns,
    int InventoryMovementCount,
    decimal QuotationValue,
    decimal SalesOrderValue,
    decimal CreditNoteTotal,
    decimal CashPayments,
    decimal CardPayments,
    decimal BankPayments,
    decimal AdvancePayments,
    decimal Refunds,
    decimal OpeningReceivable,
    decimal ClosingReceivable,
    decimal OutstandingAmount,
    DailyTransactionRefsDto TransactionRefs,
    DateTimeOffset CreatedAt);

public sealed record MonthlyTransactionRefsDto(
    IReadOnlyList<string> InvoiceIds,
    IReadOnlyList<string> PaymentIds,
    IReadOnlyList<string> PurchaseIds,
    IReadOnlyList<string> ExpenseIds,
    IReadOnlyList<string> InventorySnapshotIds,
    IReadOnlyList<string> ProductionSnapshotIds);

public sealed record MonthlyClosingSummaryDto(
    Guid Id,
    Guid MonthlyPeriodId,
    int Year,
    int Month,
    string BranchId,
    decimal SalesTotal,
    decimal PurchaseTotal,
    decimal PaymentTotal,
    decimal ExpenseTotal,
    decimal InventoryValue,
    decimal WipValue,
    decimal CostOfGoodsSold,
    decimal GrossProfit,
    decimal RawMaterials,
    decimal Labour,
    decimal Production,
    decimal Waste,
    decimal ReusableWaste,
    decimal Overhead,
    decimal CreditNotes,
    decimal NetMargin,
    MonthlyTransactionRefsDto TransactionRefs,
    DateTimeOffset CreatedAt);

public sealed record ProductionDailySnapshotDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string ProductionOrderId,
    string ProductionOrderNumber,
    string OperationId,
    string OperationName,
    string? WorkerId,
    string? WorkerName,
    decimal TotalQty,
    decimal CompletedQty,
    decimal PartialQty,
    decimal ProgressPercentage,
    int WorkedMinutes,
    decimal ProducedQty,
    decimal RejectedQty,
    string? JobStatus,
    string? TaskStatus,
    DateTimeOffset RecordedAt);

public sealed record ProductionMonthlySnapshotDto(
    Guid Id,
    Guid MonthlyPeriodId,
    int Year,
    int Month,
    string ProductionOrderId,
    string ProductionOrderNumber,
    string OperationId,
    string OperationName,
    decimal TotalQty,
    decimal CompletedQty,
    decimal WorkInProgressQty,
    decimal ProgressPercentage,
    decimal MaterialConsumed,
    decimal LaborHours,
    decimal EstimatedCost,
    decimal ActualCostToDate,
    decimal WipCost,
    DateTimeOffset RecordedAt);

public sealed record InventoryDailySnapshotDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string InventoryItemId,
    string Sku,
    string Name,
    string Unit,
    decimal OpeningQty,
    decimal Receipts,
    decimal Returns,
    decimal ProductionOutput,
    decimal Issues,
    decimal Consumption,
    decimal Deliveries,
    decimal Adjustments,
    decimal ClosingQty,
    IReadOnlyList<string> MovementIds,
    DateTimeOffset RecordedAt);

public sealed record InventoryMonthlySnapshotDto(
    Guid Id,
    Guid MonthlyPeriodId,
    int Year,
    int Month,
    string InventoryItemId,
    string Sku,
    string Name,
    string Unit,
    decimal OpeningQty,
    decimal OpeningValue,
    decimal ReceivedQty,
    decimal ReceivedValue,
    decimal ConsumedQty,
    decimal ConsumedValue,
    decimal AdjustmentQty,
    decimal AdjustmentValue,
    decimal ClosingQty,
    decimal ClosingValue,
    DateTimeOffset RecordedAt);

public sealed record WorkerSessionCheckpointDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string SessionId,
    string WorkerId,
    string WorkerName,
    string ProductionOrderId,
    string OperationId,
    string OperationName,
    decimal ProgressPercentage,
    DateTimeOffset StartedAt,
    DateTimeOffset CheckpointAt,
    string RuleApplied,
    DateTimeOffset? ResumedAt,
    string Status);

public sealed record PeriodAuditLogDto(
    Guid Id,
    string PeriodType,
    Guid PeriodId,
    string Action,
    string UserId,
    string UserName,
    string? Reason,
    IReadOnlyDictionary<string, object?>? Details,
    DateTimeOffset PerformedAt);

public sealed record PeriodAdjustmentDto(
    Guid Id,
    string BranchId,
    string PostingBusinessDate,
    Guid? PostingMonthlyPeriodId,
    string EntityType,
    string EntityId,
    string? OriginalBusinessDate,
    string? OriginalTransactionId,
    string AdjustmentType,
    decimal? QuantityDelta,
    decimal? AmountDelta,
    string Reason,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt);

public sealed record PeriodCloseSettingsDto(
    string BranchId,
    string WorkerSessionCloseRule,
    bool AllowNegativeStock,
    bool RequireAllDaysClosedForMonthlyClose,
    int FiscalYearStartMonth,
    int RequiredDailyWorkMinutes,
    bool AllowIncompleteEmployeeHoursException,
    bool CountPauseAsWorked,
    int DoubleOvertimeAfterMinutes,
    bool RequireOvertimeApproval);

public sealed record UpdatePeriodCloseSettingsCommand(
    string BranchId,
    string? WorkerSessionCloseRule = null,
    bool? AllowNegativeStock = null,
    bool? RequireAllDaysClosedForMonthlyClose = null,
    int? FiscalYearStartMonth = null,
    int? RequiredDailyWorkMinutes = null,
    bool? AllowIncompleteEmployeeHoursException = null,
    bool? CountPauseAsWorked = null,
    int? DoubleOvertimeAfterMinutes = null,
    bool? RequireOvertimeApproval = null);

public sealed record CloseDayOptions(
    bool SupervisorConfirmed = false,
    bool IncompleteHoursExceptionConfirmed = false,
    bool OvertimeApproved = false);

public sealed record ReopenPeriodCommand(string Reason);

public sealed record CreatePeriodAdjustmentCommand(
    string BranchId,
    string PostingBusinessDate,
    string EntityType,
    string EntityId,
    string AdjustmentType,
    string Reason,
    string? OriginalBusinessDate = null,
    string? OriginalTransactionId = null,
    decimal? QuantityDelta = null,
    decimal? AmountDelta = null);

public sealed record AssertWritableCommand(string BranchId, string BusinessDate);

public sealed record DayCloseWorkspaceDto(
    BusinessPeriodDto Period,
    DailyClosingSummaryDto? Summary,
    IReadOnlyList<DayCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionDailySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryDailySnapshotDto> InventorySnapshots,
    IReadOnlyList<WorkerSessionCheckpointDto> SessionCheckpoints,
    IReadOnlyList<object> EmployeeDaySummaries,
    IReadOnlyList<PeriodAuditLogDto> AuditLog,
    bool CanClose,
    bool CanReopen,
    int ActiveSessionCount,
    string WorkerSessionRule,
    int RequiredDailyWorkMinutes,
    bool AllowIncompleteEmployeeHoursException,
    int IncompleteEmployeeCount,
    int OvertimeEmployeeCount,
    int TotalOvertimeMinutes,
    bool RequireOvertimeApproval);

public sealed record MonthlyCloseWorkspaceDto(
    MonthlyPeriodDto Period,
    MonthlyClosingSummaryDto? Summary,
    IReadOnlyList<MonthlyCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionMonthlySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryMonthlySnapshotDto> InventorySnapshots,
    IReadOnlyList<BusinessPeriodDto> DayPeriods,
    IReadOnlyList<PeriodAuditLogDto> AuditLog,
    bool CanClose,
    bool CanReopen,
    int OpenDayCount,
    int ClosedDayCount);

public sealed record CloseDayResultDto(
    BusinessPeriodDto Period,
    BusinessPeriodDto NextPeriod,
    DailyClosingSummaryDto Summary,
    IReadOnlyList<DayCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionDailySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryDailySnapshotDto> InventorySnapshots,
    IReadOnlyList<WorkerSessionCheckpointDto> SessionCheckpoints);

public sealed record CloseMonthResultDto(
    MonthlyPeriodDto Period,
    MonthlyPeriodDto NextPeriod,
    MonthlyClosingSummaryDto Summary,
    IReadOnlyList<MonthlyCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionMonthlySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryMonthlySnapshotDto> InventorySnapshots);
