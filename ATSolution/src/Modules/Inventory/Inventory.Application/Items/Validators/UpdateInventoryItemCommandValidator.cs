using FluentValidation;
using Inventory.Application.Items;
using Inventory.Application.Units;
using Inventory.Application.Warehouses;
using Inventory.Domain.Common;

namespace Inventory.Application.Items.Validators;

public sealed class UpdateInventoryItemCommandValidator : AbstractValidator<UpdateInventoryItemCommand>
{
    public UpdateInventoryItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
