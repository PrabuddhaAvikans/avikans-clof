using FluentValidation;
using Inventory.Application.Reprocessing;

namespace Inventory.Application.Reprocessing.Validators;

public sealed class CompleteReprocessingCommandValidator : AbstractValidator<CompleteReprocessingCommand>
{
    public CompleteReprocessingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RecoveredQuantity).GreaterThan(0);
        RuleFor(x => x.ProcessLossQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
