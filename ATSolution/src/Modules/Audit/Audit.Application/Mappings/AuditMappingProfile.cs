using Audit.Application.AuditLogs;
using Audit.Domain.AuditLogs;
using AutoMapper;

namespace Audit.Application.Mappings;

public sealed class AuditMappingProfile : Profile
{
    public AuditMappingProfile()
    {
        CreateMap<AuditLogEntry, AuditLogEntryDto>()
            .ForMember(d => d.Changes, o => o.Ignore());
    }
}
