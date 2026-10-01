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
