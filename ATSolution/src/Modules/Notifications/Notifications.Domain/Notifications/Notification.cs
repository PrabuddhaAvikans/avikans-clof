using ATSolution.Domain.Entities.Common;
using Notifications.Domain.Common;

namespace Notifications.Domain.Notifications;

public class Notification : Entity<Guid>, IAuditableEntity
{
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string Type { get; private set; } = NotificationTypes.Info;
    public string Category { get; private set; } = NotificationCategories.System;
    public bool IsRead { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string RecipientId { get; private set; } = null!;
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Notification Create(
        string title,
        string message,
        string type,
        string category,
        string recipientId,
        string? actionUrl = null,
        string? entityType = null,
        string? entityId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Notification
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type,
            Category = category,
            IsRead = false,
            ActionUrl = actionUrl,
            EntityType = entityType,
            EntityId = entityId,
            RecipientId = recipientId.Trim(),
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
