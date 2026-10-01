using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record TaskMaterialUsageDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemSku,
    string InventoryItemName,
    decimal Quantity,
    string Unit,
    decimal? Cost);
