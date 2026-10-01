using Catalog.Application.Categories;
using Catalog.Domain.Common;
using FluentValidation;

namespace Catalog.Application.Categories.Validators;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => x.Slug is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).When(x => x.SortOrder.HasValue);
        RuleFor(x => x.Status)
            .Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive)
            .When(x => x.Status is not null);
    }
}
