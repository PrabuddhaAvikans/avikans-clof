using ATSolution.Domain.Entities.Common;
using Catalog.Domain.Common;

namespace Catalog.Domain.Brands;

public class Brand : Entity<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Website { get; private set; }
    public string? CountryOfOrigin { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Brand Create(
        string name,
        string slug,
        string? description,
        string status,
        string? logoUrl = null,
        string? website = null,
        string? countryOfOrigin = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Brand
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim(),
            Description = description,
            LogoUrl = logoUrl,
            Website = website,
            CountryOfOrigin = countryOfOrigin,
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string name,
        string slug,
        string? description,
        string status,
        string? logoUrl,
        string? website,
        string? countryOfOrigin)
    {
        Name = name.Trim();
        Slug = slug.Trim();
        Description = description;
        LogoUrl = logoUrl;
        Website = website;
        CountryOfOrigin = countryOfOrigin;
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ApplyPartial(
        string? name,
        string? slug,
        string? description,
        string? status,
        string? logoUrl,
        string? website,
        string? countryOfOrigin)
    {
        if (name is not null) Name = name.Trim();
        if (slug is not null) Slug = slug.Trim();
        if (description is not null) Description = description;
        if (status is not null) Status = status;
        if (logoUrl is not null) LogoUrl = logoUrl;
        if (website is not null) Website = website;
        if (countryOfOrigin is not null) CountryOfOrigin = countryOfOrigin;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
