using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record TaskContributorInputDto(
    Guid UserId,
    string UserName,
    decimal? ContributionPercent,
    decimal? Quantity,
    decimal? RejectedQuantity,
    decimal? WasteQuantity,
    decimal? ActualHours,
    decimal? OvertimeHours,
    decimal? NormalOvertimeHours,
    decimal? DoubleOvertimeHours,
    decimal? ProgressPercentage,
    IReadOnlyList<int>? UnitNos);
