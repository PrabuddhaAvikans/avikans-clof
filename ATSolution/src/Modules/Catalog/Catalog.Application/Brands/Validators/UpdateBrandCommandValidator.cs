using Catalog.Application.Brands;
using Catalog.Domain.Common;
using FluentValidation;

namespace Catalog.Application.Brands.Validators;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => x.Slug is not null);
        RuleFor(x => x.Status)
            .Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive)
            .When(x => x.Status is not null);
    }
}
