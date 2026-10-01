using Catalog.Application.Products;
using Catalog.Domain.Common;
using FluentValidation;

namespace Catalog.Application.Products.Validators;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ProductType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Status).Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive);
        RuleFor(x => x.MinOrderQuantity).GreaterThan(0);
        RuleFor(x => x.LeadTimeDays).GreaterThanOrEqualTo(0);
    }
}
