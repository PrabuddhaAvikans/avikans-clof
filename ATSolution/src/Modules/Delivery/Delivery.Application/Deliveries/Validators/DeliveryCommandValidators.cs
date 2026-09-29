using FluentValidation;

namespace Delivery.Application.Deliveries.Validators;

public sealed class CreateDeliveryCommandValidator : AbstractValidator<CreateDeliveryCommand>
{
    public CreateDeliveryCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.ScheduledDate).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductSku).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.Unit).NotEmpty().MaximumLength(50);
            item.RuleFor(i => i.QuantityOrdered).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.QuantityDelivered).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateDeliveryCommandValidator : AbstractValidator<UpdateDeliveryCommand>
{
    public UpdateDeliveryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().When(x => x.Items is not null);
    }
}

public sealed class UpdateDeliveryStatusCommandValidator : AbstractValidator<UpdateDeliveryStatusCommand>
{
    public UpdateDeliveryStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public sealed class RecordProofOfDeliveryCommandValidator : AbstractValidator<RecordProofOfDeliveryCommand>
{
    public RecordProofOfDeliveryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SignedBy).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SignedAt).NotEmpty();
    }
}
