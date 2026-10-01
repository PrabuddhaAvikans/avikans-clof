using FluentValidation;

namespace Delivery.Application.Deliveries.Validators;

public sealed class RecordProofOfDeliveryCommandValidator : AbstractValidator<RecordProofOfDeliveryCommand>
{
    public RecordProofOfDeliveryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SignedBy).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SignedAt).NotEmpty();
    }
}
