using ATSolution.SharedKernel.Models;
using Audit.Application.AuditLogs;

namespace Audit.Application.Abstractions;

public interface IAuditService
{
    Task<PaginatedResponse<AuditLogEntryDto>> ListAsync(AuditLogListQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuditLogSummaryDto> SummaryAsync(AuditLogListQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogEntryDto> AppendAsync(AppendAuditLogCommand command, CancellationToken cancellationToken = default);
}
