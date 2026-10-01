using FluentValidation;
using Sales.Application.Costing;

namespace Sales.Application.Costing.Validators;

public sealed class UpdateCostingNotesCommandValidator : AbstractValidator<UpdateCostingNotesCommand>
{
    public UpdateCostingNotesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).NotNull();
    }
}
