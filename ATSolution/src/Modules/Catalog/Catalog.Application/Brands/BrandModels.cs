using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Brands;

public sealed class BrandListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? Website,
    string? CountryOfOrigin,
    string Status,
    int ProductCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateBrandCommand(
    string Name,
    string? Slug,
    string? Description,
    string Status,
    string? LogoUrl = null,
    string? Website = null,
    string? CountryOfOrigin = null);

public sealed record UpdateBrandCommand(
    Guid Id,
    string? Name = null,
    string? Slug = null,
    string? Description = null,
    string? Status = null,
    string? LogoUrl = null,
    string? Website = null,
    string? CountryOfOrigin = null);
