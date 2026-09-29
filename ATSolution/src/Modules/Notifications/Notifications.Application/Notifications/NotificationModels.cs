using ATSolution.SharedKernel.Models;

namespace Notifications.Application.Notifications;

public sealed class NotificationListQuery : PaginatedRequest
{
    public bool? IsRead { get; set; }
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? RecipientId { get; set; }
}

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

public sealed record CreateNotificationCommand(
    string Title,
    string Message,
    string RecipientId,
    string Type = "info",
    string Category = "system",
    string? ActionUrl = null,
    string? EntityType = null,
    string? EntityId = null);

public sealed record MarkAllReadCommand(string RecipientId);

public sealed record UnreadCountDto(int Count);
