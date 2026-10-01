namespace PeriodClose.Application.PeriodClose;

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
