using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public NotificationService(
        INotificationRepository notifications,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<NotificationDto>> ListAsync(
        NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _notifications.Query().AsNoTracking().AsQueryable();

        if (query.IsRead.HasValue)
            items = items.Where(n => n.IsRead == query.IsRead.Value);
        if (!string.IsNullOrWhiteSpace(query.Category))
            items = items.Where(n => n.Category == query.Category);
        if (!string.IsNullOrWhiteSpace(query.Type))
            items = items.Where(n => n.Type == query.Type);
        if (!string.IsNullOrWhiteSpace(query.RecipientId))
            items = items.Where(n => n.RecipientId == query.RecipientId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(n => n.Title.Contains(search) || n.Message.Contains(search));
        }

        items = items.OrderByDescending(n => n.CreatedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<NotificationDto>.Create(pageItems.Select(Map).ToList(), totalCount, page, pageSize);
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _notifications.GetByIdAsync(id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var notification = Notification.Create(
            command.Title,
            command.Message,
            command.Type,
            command.Category,
            command.RecipientId,
            command.ActionUrl,
            command.EntityType,
            command.EntityId);

        await _notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(notification);
    }

    public async Task<NotificationDto> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetByIdAsync(id, cancellationToken, asNoTracking: false)
            ?? throw new NotFoundException($"Notification '{id}' was not found.");

        notification.MarkAsRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(notification);
    }

    public async Task MarkAllAsReadAsync(
        MarkAllReadCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var unread = await _notifications.ListAsync(
            n => n.RecipientId == command.RecipientId && !n.IsRead,
            cancellationToken,
            asNoTracking: false);

        foreach (var notification in unread)
            notification.MarkAsRead();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(
        string recipientId,
        CancellationToken cancellationToken = default)
    {
        var count = await _notifications.CountAsync(
            n => n.RecipientId == recipientId && !n.IsRead,
            cancellationToken);
        return new UnreadCountDto(count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetByIdAsync(id, cancellationToken, asNoTracking: false)
            ?? throw new NotFoundException($"Notification '{id}' was not found.");

        _notifications.Remove(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDto Map(Notification n) =>
        new(
            n.Id,
            n.Title,
            n.Message,
            n.Type,
            n.Category,
            n.IsRead,
            n.ActionUrl,
            n.EntityType,
            n.EntityId,
            n.RecipientId,
            n.CreatedOnUtc,
            n.ReadAtUtc);
}
