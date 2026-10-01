using ATSolution.Application.Abstractions.Persistence;
using Manufacturing.Application.Jobs;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Work;
using Microsoft.EntityFrameworkCore;

namespace Manufacturing.Application.Services;

internal static class WorkSessionSync
{
    public static async Task ApplyAsync(
        IRepository<EmployeeWorkSession, Guid> sessions,
        ManufacturingJob job,
        ManufacturingTaskActionDto action,
        TaskActionActorDto actor,
        CancellationToken cancellationToken)
    {
        if (!action.TaskId.HasValue)
            return;

        var task = job.Tasks.FirstOrDefault(t => t.Id == action.TaskId.Value);
        if (task is null)
            return;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var open = await sessions.Query()
            .Where(s => s.TaskId == task.Id && s.EndedAtUtc == null)
            .ToListAsync(cancellationToken);

        switch (action.Type)
        {
            case "start":
            case "resume":
                var people = (action.Contributors ?? [])
                    .Select(person => (person.UserId, person.UserName))
                    .DefaultIfEmpty((actor.UserId, actor.UserName))
                    .GroupBy(person => person.UserId)
                    .Select(group => group.First())
                    .ToList();
                foreach (var person in people)
                {
                    if (open.Any(s => s.UserId == person.UserId && s.BusinessDate == today))
                        continue;
                    await sessions.AddAsync(
                        EmployeeWorkSession.Start(job.Id, task.Id, person.UserId, person.UserName, now),
                        cancellationToken);
                }
                break;
            case "pause":
            case "hold":
                foreach (var session in open)
                    session.Pause(now, action.Notes);
                break;
            case "complete":
            case "skip":
                if (task.Status is ManufacturingTaskStatuses.Completed
                    or ManufacturingTaskStatuses.Skipped
                    or ManufacturingTaskStatuses.Cancelled)
                {
                    foreach (var session in open)
                        session.Complete(now);
                }
                break;
        }
    }
}
