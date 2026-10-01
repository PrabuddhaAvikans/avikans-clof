using ATSolution.SharedKernel.Models;

namespace Notifications.Application.Notifications;

public sealed record CreateNotificationCommand(
    string Title,
    string Message,
    string RecipientId,
    string Type = "info",
    string Category = "system",
    string? ActionUrl = null,
    string? EntityType = null,
    string? EntityId = null);
