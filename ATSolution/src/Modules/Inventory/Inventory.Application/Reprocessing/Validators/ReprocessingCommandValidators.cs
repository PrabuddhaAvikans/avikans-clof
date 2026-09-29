using FluentValidation;
using Inventory.Application.Reprocessing;

namespace Inventory.Application.Reprocessing.Validators;

public sealed class CreateReprocessingBatchCommandValidator : AbstractValidator<CreateReprocessingBatchCommand>
{
    public CreateReprocessingBatchCommandValidator()
    {
        RuleFor(x => x.InputScrapLotId).NotEmpty();
        RuleFor(x => x.InputQuantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
        RuleFor(x => x.SourceProductionOrderId).MaximumLength(100).When(x => x.SourceProductionOrderId is not null);
    }
}

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

public sealed class CancelReprocessingCommandValidator : AbstractValidator<CancelReprocessingCommand>
{
    public CancelReprocessingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => x.Reason is not null);
    }
}
