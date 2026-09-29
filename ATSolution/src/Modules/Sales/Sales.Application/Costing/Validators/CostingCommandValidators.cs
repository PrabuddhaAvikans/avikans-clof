using FluentValidation;
using Sales.Application.Costing;

namespace Sales.Application.Costing.Validators;

public sealed class SubmitCoatingCommandValidator : AbstractValidator<SubmitCoatingCommand>
{
    public SubmitCoatingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}

public sealed class CostingDecisionCommandValidator : AbstractValidator<CostingDecisionCommand>
{
    public CostingDecisionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CostingCommentCommandValidator : AbstractValidator<CostingCommentCommand>
{
    public CostingCommentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(4000);
    }
}

public sealed class UpdateCostingNotesCommandValidator : AbstractValidator<UpdateCostingNotesCommand>
{
    public UpdateCostingNotesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).NotNull();
    }
}
