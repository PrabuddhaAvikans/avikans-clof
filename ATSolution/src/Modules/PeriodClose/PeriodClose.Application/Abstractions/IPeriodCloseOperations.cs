using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public interface IPeriodCloseOperations
{
    Task<DayOperationalPicture> LoadDayAsync(
        string businessDate,
        PeriodClosePolicy policy,
        CancellationToken cancellationToken = default);

    Task<MonthOperationalPicture> LoadMonthAsync(
        int year,
        int month,
        bool allowNegativeStock,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses in-progress tasks, assignments, and work sessions for the business date.
    /// Returns the sessions that were checkpointed.
    /// </summary>
    Task<IReadOnlyList<SessionCheckpointSeed>> PauseOpenWorkAsync(
        string businessDate,
        string reason,
        CancellationToken cancellationToken = default);
}
