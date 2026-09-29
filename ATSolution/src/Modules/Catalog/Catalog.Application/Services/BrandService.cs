using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Application.Abstractions;
using Catalog.Application.Brands;
using Catalog.Domain.Brands;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Services;

public sealed class BrandService : IBrandService
{
    private readonly IRepository<Brand, Guid> _brands;
    private readonly IRepository<Product, Guid> _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public BrandService(
        IRepository<Brand, Guid> brands,
        IRepository<Product, Guid> products,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _brands = brands;
        _products = products;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<BrandDto>> ListAsync(
        BrandListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);

        var brands = _brands.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            brands = brands.Where(b => b.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            brands = brands.Where(b =>
                b.Name.Contains(search)
                || b.Slug.Contains(search)
                || (b.Description != null && b.Description.Contains(search)));
        }

        brands = query.SortDirection == "desc"
            ? brands.OrderByDescending(b => b.Name)
            : brands.OrderBy(b => b.Name);

        var totalCount = await brands.CountAsync(cancellationToken);
        var items = await brands
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResponse<BrandDto>.Create(
            await MapManyAsync(items, cancellationToken),
            totalCount,
            page,
            pageSize);
    }

    public async Task<BrandDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _brands.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return brand is null ? null : (await MapManyAsync([brand], cancellationToken))[0];
    }

    public async Task<BrandDto> CreateAsync(
        CreateBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var slug = string.IsNullOrWhiteSpace(command.Slug)
            ? Slugify(command.Name)
            : command.Slug.Trim().ToLowerInvariant();

        var slugExists = await _brands.Query()
            .AnyAsync(b => b.Slug == slug, cancellationToken);
        if (slugExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Slug), "Brand slug already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var brand = Brand.Create(
            command.Name,
            slug,
            command.Description,
            command.Status,
            command.LogoUrl,
            command.Website,
            command.CountryOfOrigin);

        await _brands.AddAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(brand.Id, cancellationToken))!;
    }

    public async Task<BrandDto> UpdateAsync(
        UpdateBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var brand = await _brands.Query()
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Brand '{command.Id}' was not found.");

        if (command.Slug is not null)
        {
            var slugExists = await _brands.Query()
                .AnyAsync(b => b.Slug == command.Slug && b.Id != command.Id, cancellationToken);
            if (slugExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Slug), "Brand slug already exists.", ValidationErrorCodes.Conflict),
                ]);
            }
        }

        brand.ApplyPartial(
            command.Name,
            command.Slug,
            command.Description,
            command.Status,
            command.LogoUrl,
            command.Website,
            command.CountryOfOrigin);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(brand.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _brands.Query()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Brand '{id}' was not found.");

        brand.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<BrandDto>> MapManyAsync(
        IReadOnlyList<Brand> brands,
        CancellationToken cancellationToken)
    {
        if (brands.Count == 0)
        {
            return [];
        }

        var brandIds = brands.Select(b => b.Id).ToList();
        var productCounts = await _products.Query()
            .AsNoTracking()
            .Where(p => p.BrandId != null && brandIds.Contains(p.BrandId.Value) && p.Status == EntityStatuses.Active)
            .GroupBy(p => p.BrandId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return brands.Select(b => new BrandDto(
            b.Id,
            b.Name,
            b.Slug,
            b.Description,
            b.LogoUrl,
            b.Website,
            b.CountryOfOrigin,
            b.Status,
            productCounts.GetValueOrDefault(b.Id),
            b.CreatedOnUtc,
            b.ModifiedOnUtc)).ToList();
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
