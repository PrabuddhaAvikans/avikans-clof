using FluentValidation;
using Inventory.Application.Items;
using Inventory.Application.Units;
using Inventory.Application.Warehouses;
using Inventory.Domain.Common;

namespace Inventory.Application.Items.Validators;

public sealed class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ItemType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Warehouse).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive);
    }
}
