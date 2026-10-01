using FluentValidation;
using Sales.Application.Quotations;
using Sales.Domain.Common;

namespace Sales.Application.Quotations.Validators;

public sealed class AddQuotationContactCommandValidator : AbstractValidator<AddQuotationContactCommand>
{
    public AddQuotationContactCommandValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
    }
}
