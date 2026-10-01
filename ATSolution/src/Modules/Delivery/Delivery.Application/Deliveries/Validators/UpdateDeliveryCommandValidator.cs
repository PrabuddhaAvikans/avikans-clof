using FluentValidation;

namespace Delivery.Application.Deliveries.Validators;

public sealed class UpdateDeliveryCommandValidator : AbstractValidator<UpdateDeliveryCommand>
{
    public UpdateDeliveryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().When(x => x.Items is not null);
    }
}
