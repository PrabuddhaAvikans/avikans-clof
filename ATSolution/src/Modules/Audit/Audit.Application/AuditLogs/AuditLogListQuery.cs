using ATSolution.SharedKernel.Models;

namespace Audit.Application.AuditLogs;

public sealed class AuditLogListQuery : PaginatedRequest
{
    public string? Entity { get; set; }
    public string? Action { get; set; }
    public string? Severity { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
