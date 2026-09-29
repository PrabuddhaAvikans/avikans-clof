using ATSolution.SharedKernel.Models;
using Inventory.Application.Warehouses;

namespace Inventory.Application.Abstractions;

public interface IWarehouseService
{
    Task<PaginatedResponse<WarehouseDto>> ListAsync(WarehouseListQuery query, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateAsync(CreateWarehouseCommand command, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateAsync(UpdateWarehouseCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
