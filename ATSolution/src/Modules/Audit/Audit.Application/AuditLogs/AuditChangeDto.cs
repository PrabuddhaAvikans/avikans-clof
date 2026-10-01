namespace Audit.Application.AuditLogs;

public sealed record AuditChangeDto(string Field, string? From = null, string? To = null);
