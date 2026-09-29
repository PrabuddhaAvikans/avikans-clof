using FluentValidation;

namespace Manufacturing.Application.Jobs.Validators;

public sealed class CreateManufacturingJobCommandValidator : AbstractValidator<CreateManufacturingJobCommand>
{
    public CreateManufacturingJobCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.PlannedStartDate).NotEmpty();
        RuleFor(x => x.PlannedEndDate).NotEmpty();
    }
}

public sealed class UpdateManufacturingJobCommandValidator : AbstractValidator<UpdateManufacturingJobCommand>
{
    public UpdateManufacturingJobCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
