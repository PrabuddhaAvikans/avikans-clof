using ATSolution.SharedKernel.Models;
using Inventory.Application.Items;

namespace Inventory.Application.Abstractions;

public interface IInventoryService
{
    Task<PaginatedResponse<InventoryItemDto>> ListAsync(InventoryListQuery query, CancellationToken cancellationToken = default);
    Task<InventoryItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> CreateAsync(CreateInventoryItemCommand command, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> UpdateAsync(UpdateInventoryItemCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResponse<StockMovementDto>> ListMovementsAsync(StockMovementListQuery query, CancellationToken cancellationToken = default);
    Task<StockMovementDto> RecordMovementAsync(RecordStockMovementCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryPriceHistoryDto>> GetPriceHistoryAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);
    Task<InventoryItemDto?> FindBySkuAsync(string sku, CancellationToken cancellationToken = default);
}
