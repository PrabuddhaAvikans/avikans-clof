using FluentValidation;
using Notifications.Application.Notifications;
using Notifications.Domain.Common;

namespace Notifications.Application.Notifications.Validators;

public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    private static readonly string[] Types =
    [
        NotificationTypes.Info,
        NotificationTypes.Success,
        NotificationTypes.Warning,
        NotificationTypes.Error,
        NotificationTypes.System,
    ];

    private static readonly string[] Categories =
    [
        NotificationCategories.Quotation,
        NotificationCategories.SalesOrder,
        NotificationCategories.Manufacturing,
        NotificationCategories.Delivery,
        NotificationCategories.Inventory,
        NotificationCategories.System,
    ];

    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RecipientId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).Must(t => Types.Contains(t));
        RuleFor(x => x.Category).Must(c => Categories.Contains(c));
        RuleFor(x => x.ActionUrl).MaximumLength(1000).When(x => x.ActionUrl is not null);
        RuleFor(x => x.EntityType).MaximumLength(100).When(x => x.EntityType is not null);
        RuleFor(x => x.EntityId).MaximumLength(100).When(x => x.EntityId is not null);
    }
}

public sealed class MarkAllReadCommandValidator : AbstractValidator<MarkAllReadCommand>
{
    public MarkAllReadCommandValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty().MaximumLength(100);
    }
}
