using ATSolution.Domain.Entities.Common;
using PeriodClose.Domain.Common;

namespace PeriodClose.Domain.Audit;

public class PeriodAdjustment : Entity<Guid>
{
    public string BranchId { get; private set; } = PeriodCloseDefaults.DefaultBranchId;
    public string PostingBusinessDate { get; private set; } = null!;
    public Guid? PostingMonthlyPeriodId { get; private set; }
    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string? OriginalBusinessDate { get; private set; }
    public string? OriginalTransactionId { get; private set; }
    public string AdjustmentType { get; private set; } = null!;
    public decimal? QuantityDelta { get; private set; }
    public decimal? AmountDelta { get; private set; }
    public string Reason { get; private set; } = null!;
    public string CreatedBy { get; private set; } = PeriodCloseDefaults.SystemUserId;
    public string CreatedByName { get; private set; } = PeriodCloseDefaults.SystemUserName;
    public DateTimeOffset CreatedAt { get; private set; }

    public static PeriodAdjustment Create(
        string branchId,
        string postingBusinessDate,
        Guid? postingMonthlyPeriodId,
        string entityType,
        string entityId,
        string? originalBusinessDate,
        string? originalTransactionId,
        string adjustmentType,
        decimal? quantityDelta,
        decimal? amountDelta,
        string reason,
        string createdBy,
        string createdByName)
    {
        return new PeriodAdjustment
        {
            Id = Guid.NewGuid(),
            BranchId = string.IsNullOrWhiteSpace(branchId)
                ? PeriodCloseDefaults.DefaultBranchId
                : branchId.Trim(),
            PostingBusinessDate = postingBusinessDate,
            PostingMonthlyPeriodId = postingMonthlyPeriodId,
            EntityType = entityType,
            EntityId = entityId,
            OriginalBusinessDate = originalBusinessDate,
            OriginalTransactionId = originalTransactionId,
            AdjustmentType = adjustmentType,
            QuantityDelta = quantityDelta,
            AmountDelta = amountDelta,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
