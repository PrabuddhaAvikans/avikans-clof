namespace Notifications.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string Category,
    bool IsRead,
    string? ActionUrl,
    string? EntityType,
    string? EntityId,
    string RecipientId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
