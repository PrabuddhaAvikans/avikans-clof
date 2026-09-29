using ATSolution.SharedKernel.Models;
using Notifications.Application.Notifications;

namespace Notifications.Application.Abstractions;

public interface INotificationService
{
    Task<PaginatedResponse<NotificationDto>> ListAsync(
        NotificationListQuery query,
        CancellationToken cancellationToken = default);

    Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NotificationDto> CreateAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default);

    Task<NotificationDto> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(MarkAllReadCommand command, CancellationToken cancellationToken = default);

    Task<UnreadCountDto> GetUnreadCountAsync(string recipientId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
