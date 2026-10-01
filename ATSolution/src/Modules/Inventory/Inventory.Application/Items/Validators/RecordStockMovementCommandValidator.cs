using FluentValidation;
using Inventory.Application.Items;
using Inventory.Application.Units;
using Inventory.Application.Warehouses;
using Inventory.Domain.Common;

namespace Inventory.Application.Items.Validators;

public sealed class RecordStockMovementCommandValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty();
    }
}
