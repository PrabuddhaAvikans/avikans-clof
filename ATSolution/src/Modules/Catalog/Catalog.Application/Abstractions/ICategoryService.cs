using ATSolution.SharedKernel.Models;
using Catalog.Application.Categories;

namespace Catalog.Application.Abstractions;

public interface ICategoryService
{
    Task<PaginatedResponse<CategoryDto>> ListAsync(CategoryListQuery query, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
