using FluentValidation;
using Notifications.Application.Notifications;
using Notifications.Domain.Common;

namespace Notifications.Application.Notifications.Validators;

public sealed class MarkAllReadCommandValidator : AbstractValidator<MarkAllReadCommand>
{
    public MarkAllReadCommandValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty().MaximumLength(100);
    }
}
