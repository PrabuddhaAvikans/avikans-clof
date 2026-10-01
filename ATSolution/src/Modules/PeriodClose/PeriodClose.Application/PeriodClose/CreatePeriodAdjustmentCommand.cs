using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

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
