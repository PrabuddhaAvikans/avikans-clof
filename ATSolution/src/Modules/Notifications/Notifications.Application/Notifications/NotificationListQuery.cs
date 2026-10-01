using ATSolution.SharedKernel.Models;

namespace Notifications.Application.Notifications;

public sealed class NotificationListQuery : PaginatedRequest
{
    public bool? IsRead { get; set; }
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? RecipientId { get; set; }
}
