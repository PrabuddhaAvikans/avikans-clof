using Catalog.Application.Categories;
using Catalog.Domain.Common;
using FluentValidation;

namespace Catalog.Application.Categories.Validators;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive);
    }
}
