using AutoMapper;
using Inventory.Application.Items;
using Inventory.Application.Units;
using Inventory.Application.Warehouses;
using Inventory.Domain.Items;
using Inventory.Domain.Units;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Mappings;

public sealed class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<Warehouse, WarehouseDto>()
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc));

        CreateMap<UnitOfMeasure, UnitOfMeasureDto>()
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc));

        CreateMap<InventoryItem, InventoryItemDto>()
            .ForMember(d => d.UnitCost, o => o.MapFrom(s => s.CostPrice))
            .ForMember(d => d.QuantityAvailable, o => o.MapFrom(s => s.QuantityAvailable))
            .ForMember(d => d.LastRestockedAt, o => o.MapFrom(s => s.LastRestockedAtUtc))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc))
            .ForMember(d => d.PricingEffectiveDate, o => o.MapFrom(s => s.PricingEffectiveDate));
    }
}
