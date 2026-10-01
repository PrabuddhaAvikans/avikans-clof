using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Brands;

public sealed record CreateBrandCommand(
    string Name,
    string? Slug,
    string? Description,
    string Status,
    string? LogoUrl = null,
    string? Website = null,
    string? CountryOfOrigin = null);
