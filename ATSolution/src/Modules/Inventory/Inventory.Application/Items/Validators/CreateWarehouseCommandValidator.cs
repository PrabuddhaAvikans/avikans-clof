using FluentValidation;
using Inventory.Application.Items;
using Inventory.Application.Units;
using Inventory.Application.Warehouses;
using Inventory.Domain.Common;

namespace Inventory.Application.Items.Validators;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive);
    }
}
