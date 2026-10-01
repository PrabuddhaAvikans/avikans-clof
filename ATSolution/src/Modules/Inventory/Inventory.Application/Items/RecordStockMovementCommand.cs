using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Items;

public sealed record RecordStockMovementCommand(
    Guid InventoryItemId,
    string Type,
    decimal Quantity,
    string? ReferenceType = null,
    string? ReferenceId = null,
    string? Notes = null,
    JsonElement? Trace = null,
    string? PerformedBy = null,
    string? PerformedByName = null);
