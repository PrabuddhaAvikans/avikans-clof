namespace Catalog.Application.Brands;

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
