using FluentValidation;
using Sales.Application.Quotations;
using Sales.Domain.Common;

namespace Sales.Application.Quotations.Validators;

public sealed class CreateQuotationCommandValidator : AbstractValidator<CreateQuotationCommand>
{
    public CreateQuotationCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty();
        RuleForEach(x => x.LineItems).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductName).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.ProductSku).NotEmpty().MaximumLength(100);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateQuotationCommandValidator : AbstractValidator<UpdateQuotationCommand>
{
    public UpdateQuotationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty().When(x => x.LineItems is not null);
    }
}

public sealed class AddQuotationContactCommandValidator : AbstractValidator<AddQuotationContactCommand>
{
    public AddQuotationContactCommandValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
    }
}
