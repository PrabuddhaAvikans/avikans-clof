using FluentValidation;
using Sales.Application.Costing;

namespace Sales.Application.Costing.Validators;

public sealed class CostingDecisionCommandValidator : AbstractValidator<CostingDecisionCommand>
{
    public CostingDecisionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
