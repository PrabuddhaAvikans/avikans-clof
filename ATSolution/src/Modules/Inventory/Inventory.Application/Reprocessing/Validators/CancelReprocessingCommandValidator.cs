using FluentValidation;
using Inventory.Application.Reprocessing;

namespace Inventory.Application.Reprocessing.Validators;

public sealed class CancelReprocessingCommandValidator : AbstractValidator<CancelReprocessingCommand>
{
    public CancelReprocessingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => x.Reason is not null);
    }
}
