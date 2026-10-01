using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications;

namespace Notifications.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Notifications.Base)]
[ApiController]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NotificationDto>>> List(
        [FromQuery] NotificationListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<NotificationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _notificationService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet(ApiRoutes.Notifications.UnreadCount)]
    public async Task<ActionResult<UnreadCountDto>> UnreadCount(
        [FromQuery] string recipientId,
        CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.GetUnreadCountAsync(recipientId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<NotificationDto>> Create(
        [FromBody] CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var item = await _notificationService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPost(ApiRoutes.Notifications.Read)]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.MarkAsReadAsync(id, cancellationToken));
    }

    [HttpPost(ApiRoutes.Notifications.MarkAllRead)]
    public async Task<IActionResult> MarkAllAsRead(
        [FromBody] MarkAllReadCommand command,
        CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete(ApiRoutes.ById)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Notification deleted successfully."));
    }
}
