using ATSolution.SharedKernel.Models;
using Catalog.Application.Brands;

namespace Catalog.Application.Abstractions;

public interface IBrandService
{
    Task<PaginatedResponse<BrandDto>> ListAsync(BrandListQuery query, CancellationToken cancellationToken = default);
    Task<BrandDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateAsync(CreateBrandCommand command, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateAsync(UpdateBrandCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
