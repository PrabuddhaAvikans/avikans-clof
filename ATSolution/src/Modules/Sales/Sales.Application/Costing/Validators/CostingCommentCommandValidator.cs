using FluentValidation;
using Sales.Application.Costing;

namespace Sales.Application.Costing.Validators;

public sealed class CostingCommentCommandValidator : AbstractValidator<CostingCommentCommand>
{
    public CostingCommentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(4000);
    }
}
