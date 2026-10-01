namespace Audit.Application.AuditLogs;

public sealed record AuditLogSummaryDto(
    int Total,
    int Info,
    int Warning,
    int Critical,
    int Today);
