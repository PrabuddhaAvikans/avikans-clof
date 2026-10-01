using Catalog.Application.Products;
using Catalog.Domain.Common;
using FluentValidation;

namespace Catalog.Application.Products.Validators;

public sealed class UpdateProductVersionCommandValidator : AbstractValidator<UpdateProductVersionCommand>
{
    public UpdateProductVersionCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}
