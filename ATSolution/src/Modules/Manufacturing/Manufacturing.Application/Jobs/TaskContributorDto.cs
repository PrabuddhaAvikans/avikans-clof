using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record TaskContributorDto(
    Guid UserId,
    string UserName,
    decimal ContributionPercent,
    decimal Quantity,
    decimal RejectedQuantity,
    decimal WasteQuantity,
    decimal ActualHours,
    decimal OvertimeHours,
    decimal NormalOvertimeHours,
    decimal DoubleOvertimeHours,
    decimal LaborCost,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt,
    decimal? ProgressPercentage,
    decimal? CompletedQuantity,
    decimal? InProgressQuantity);
