using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Catalog.Domain.Categories;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly IRepository<Category, Guid> _categories;
    private readonly IRepository<Product, Guid> _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public CategoryService(
        IRepository<Category, Guid> categories,
        IRepository<Product, Guid> products,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _categories = categories;
        _products = products;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<CategoryDto>> ListAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);

        var categories = _categories.Query().AsNoTracking().AsQueryable();

        if (query.ParentIdIsNull == true)
        {
            categories = categories.Where(c => c.ParentId == null);
        }
        else if (query.ParentId.HasValue)
        {
            categories = categories.Where(c => c.ParentId == query.ParentId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            categories = categories.Where(c => c.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            categories = categories.Where(c =>
                c.Name.Contains(search)
                || c.Slug.Contains(search)
                || (c.Description != null && c.Description.Contains(search)));
        }

        categories = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => query.SortDirection == "desc"
                ? categories.OrderByDescending(c => c.Name)
                : categories.OrderBy(c => c.Name),
            "sortorder" => query.SortDirection == "desc"
                ? categories.OrderByDescending(c => c.SortOrder)
                : categories.OrderBy(c => c.SortOrder),
            _ => categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
        };

        var totalCount = await categories.CountAsync(cancellationToken);
        var items = await categories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mapped = await MapManyAsync(items, cancellationToken);
        return PaginatedResponse<CategoryDto>.Create(mapped, totalCount, page, pageSize);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category is null ? null : (await MapManyAsync([category], cancellationToken))[0];
    }

    public async Task<IReadOnlyList<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.Query()
            .AsNoTracking()
            .Where(c => c.Status == EntityStatuses.Active)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return await MapManyAsync(categories, cancellationToken);
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        await EnsureParentExistsAsync(command.ParentId, cancellationToken);

        var slug = string.IsNullOrWhiteSpace(command.Slug)
            ? Slugify(command.Name)
            : command.Slug.Trim().ToLowerInvariant();

        var slugExists = await _categories.Query()
            .AnyAsync(c => c.Slug == slug, cancellationToken);
        if (slugExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Slug), "Category slug already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var category = Category.Create(
            command.Name,
            slug,
            command.Description,
            command.ParentId,
            command.SortOrder,
            command.Status,
            command.ImageUrl);

        await _categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(category.Id, cancellationToken))!;
    }

    public async Task<CategoryDto> UpdateAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var category = await _categories.Query()
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Category '{command.Id}' was not found.");

        if (command.ClearParent)
        {
            // ok
        }
        else if (command.ParentId.HasValue)
        {
            if (command.ParentId == command.Id)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.ParentId), "Category cannot be its own parent.", "Validation"),
                ]);
            }

            await EnsureParentExistsAsync(command.ParentId, cancellationToken);
        }

        if (command.Slug is not null)
        {
            var slugExists = await _categories.Query()
                .AnyAsync(c => c.Slug == command.Slug && c.Id != command.Id, cancellationToken);
            if (slugExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Slug), "Category slug already exists.", ValidationErrorCodes.Conflict),
                ]);
            }
        }

        category.ApplyPartial(
            command.Name,
            command.Slug,
            command.Description,
            command.ParentId,
            command.ClearParent,
            command.SortOrder,
            command.Status,
            command.ImageUrl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(category.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.Query()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Category '{id}' was not found.");

        category.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureParentExistsAsync(Guid? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        var exists = await _categories.Query()
            .AnyAsync(c => c.Id == parentId.Value, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Parent category '{parentId}' was not found.");
        }
    }

    private async Task<IReadOnlyList<CategoryDto>> MapManyAsync(
        IReadOnlyList<Category> categories,
        CancellationToken cancellationToken)
    {
        if (categories.Count == 0)
        {
            return [];
        }

        var parentIds = categories
            .Where(c => c.ParentId.HasValue)
            .Select(c => c.ParentId!.Value)
            .Distinct()
            .ToList();

        var parents = parentIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _categories.Query()
                .AsNoTracking()
                .Where(c => parentIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var categoryIds = categories.Select(c => c.Id).ToList();
        var productCounts = await _products.Query()
            .AsNoTracking()
            .Where(p => categoryIds.Contains(p.CategoryId) && p.Status == EntityStatuses.Active)
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.ParentId,
            c.ParentId is Guid pid && parents.TryGetValue(pid, out var parentName) ? parentName : null,
            c.SortOrder,
            c.Status,
            productCounts.GetValueOrDefault(c.Id),
            c.ImageUrl,
            c.CreatedOnUtc,
            c.ModifiedOnUtc)).ToList();
    }

    private static string Slugify(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }
}
