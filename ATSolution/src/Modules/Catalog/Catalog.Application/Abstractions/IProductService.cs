using ATSolution.SharedKernel.Models;
using Catalog.Application.Products;

namespace Catalog.Application.Abstractions;

public interface IProductService
{
    Task<PaginatedResponse<ProductDto>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductVersionDto?> GetVersionAsync(Guid productId, Guid versionId, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateVersionAsync(UpdateProductVersionCommand command, CancellationToken cancellationToken = default);
    Task<ProductDto> ReviseVersionAsync(ReviseProductVersionCommand command, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateHeaderAsync(UpdateProductHeaderCommand command, CancellationToken cancellationToken = default);
}
