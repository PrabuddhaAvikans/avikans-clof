using AutoMapper;
using Notifications.Application.Notifications;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Mappings;

public sealed class NotificationsMappingProfile : Profile
{
    public NotificationsMappingProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.ReadAt, o => o.MapFrom(s => s.ReadAtUtc));
    }
}
