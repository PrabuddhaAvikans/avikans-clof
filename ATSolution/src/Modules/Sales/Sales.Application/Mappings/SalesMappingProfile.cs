using AutoMapper;
using Sales.Application.Quotations;
using Sales.Domain.Quotations;

namespace Sales.Application.Mappings;

public sealed class SalesMappingProfile : Profile
{
    public SalesMappingProfile()
    {
        CreateMap<Quotation, QuotationDto>()
            .ForMember(d => d.QuotationNumber, o => o.MapFrom(s => s.Number))
            .ForMember(d => d.LineItems, o => o.Ignore())
            .ForMember(d => d.ContactHistory, o => o.Ignore())
            .ForMember(d => d.Attachments, o => o.Ignore())
            .ForMember(d => d.Revisions, o => o.Ignore())
            .ForMember(d => d.BillingAddress, o => o.Ignore())
            .ForMember(d => d.ShippingAddress, o => o.Ignore())
            .ForMember(d => d.TermsAndConditions, o => o.MapFrom(s => s.Terms))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc));
    }
}
