using AutoMapper;
using Catalog.Application.Brands;
using Catalog.Application.Categories;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;

namespace Catalog.Application.Mappings;

public sealed class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc))
            .ForMember(d => d.ParentName, o => o.Ignore())
            .ForMember(d => d.ProductCount, o => o.Ignore());

        CreateMap<Brand, BrandDto>()
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc))
            .ForMember(d => d.ProductCount, o => o.Ignore());
    }
}
