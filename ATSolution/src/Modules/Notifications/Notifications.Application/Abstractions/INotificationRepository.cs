using ATSolution.Application.Abstractions.Persistence;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository : IRepository<Notification, Guid>
{
}
