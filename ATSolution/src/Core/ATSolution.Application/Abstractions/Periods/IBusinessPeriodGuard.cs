namespace ATSolution.Application.Abstractions.Periods;

/// <summary>
/// Rejects operational writes that fall on a closed business day or a closed month.
/// </summary>
public interface IBusinessPeriodGuard
{
    Task EnsureWritableAsync(DateTimeOffset occurredAtUtc, CancellationToken cancellationToken = default);
}
