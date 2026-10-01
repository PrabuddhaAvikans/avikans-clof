using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Brands;

public sealed record UpdateBrandCommand(
    Guid Id,
    string? Name = null,
    string? Slug = null,
    string? Description = null,
    string? Status = null,
    string? LogoUrl = null,
    string? Website = null,
    string? CountryOfOrigin = null);
