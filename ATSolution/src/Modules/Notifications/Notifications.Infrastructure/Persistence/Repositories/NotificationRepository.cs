using ATSolution.Infrastructure.Persistence.Data;
using ATSolution.Infrastructure.Persistence.Repositories;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(SqlDbContext dbContext)
    : Repository<Notification, Guid>(dbContext), INotificationRepository
{
}
