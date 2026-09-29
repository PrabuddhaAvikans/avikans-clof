using AutoMapper;
using Customers.Application.Customers;
using Customers.Domain.Customers;

namespace Customers.Application.Mappings;

public sealed class CustomersMappingProfile : Profile
{
    public CustomersMappingProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.BillingAddresses, o => o.Ignore())
            .ForMember(d => d.ShippingAddresses, o => o.Ignore())
            .ForMember(d => d.ContactPersons, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc));
    }
}
