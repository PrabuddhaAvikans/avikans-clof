namespace Catalog.Api.DTOs.Requests;

public sealed record UpdateBrandRequestDto
{
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? LogoUrl { get; init; }
    public string? Website { get; init; }
    public string? CountryOfOrigin { get; init; }
}
