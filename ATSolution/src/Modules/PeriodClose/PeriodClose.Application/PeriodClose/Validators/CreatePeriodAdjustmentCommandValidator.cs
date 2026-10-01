using FluentValidation;
using PeriodClose.Domain.Common;

namespace PeriodClose.Application.PeriodClose.Validators;

public sealed class CreatePeriodAdjustmentCommandValidator : AbstractValidator<CreatePeriodAdjustmentCommand>
{
    public CreatePeriodAdjustmentCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostingBusinessDate).NotEmpty().MaximumLength(10);
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdjustmentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}
