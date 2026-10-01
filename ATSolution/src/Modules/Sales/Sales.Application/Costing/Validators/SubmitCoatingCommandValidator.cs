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
