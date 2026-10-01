using Catalog.Application.Products;
using Catalog.Domain.Common;
using FluentValidation;

namespace Catalog.Application.Products.Validators;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Sku).MaximumLength(100).When(x => x.Sku is not null);
        RuleFor(x => x.Name).MaximumLength(300).When(x => x.Name is not null);
        RuleFor(x => x.Status)
            .Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive)
            .When(x => x.Status is not null);
    }
}
