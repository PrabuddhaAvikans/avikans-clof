using FluentValidation;

namespace Manufacturing.Application.Jobs.Validators;

public sealed class UpdateManufacturingJobCommandValidator : AbstractValidator<UpdateManufacturingJobCommand>
{
    public UpdateManufacturingJobCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
