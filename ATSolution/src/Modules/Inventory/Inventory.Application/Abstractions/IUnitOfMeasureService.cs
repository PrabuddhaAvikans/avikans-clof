using ATSolution.SharedKernel.Models;
using Inventory.Application.Units;

namespace Inventory.Application.Abstractions;

public interface IUnitOfMeasureService
{
    Task<PaginatedResponse<UnitOfMeasureDto>> ListAsync(UnitOfMeasureListQuery query, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureCommand command, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto> UpdateAsync(UpdateUnitOfMeasureCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
