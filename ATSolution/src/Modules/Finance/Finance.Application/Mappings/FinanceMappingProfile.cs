using AutoMapper;
using Finance.Application.CreditNotes;
using Finance.Application.Invoices;
using Finance.Domain.CreditNotes;
using Finance.Domain.Invoices;

namespace Finance.Application.Mappings;

public sealed class FinanceMappingProfile : Profile
{
    public FinanceMappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.LineItems, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc));

        CreateMap<CreditNote, CreditNoteDto>()
            .ForMember(d => d.LineItems, o => o.Ignore())
            .ForMember(d => d.Applications, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedOnUtc))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.ModifiedOnUtc));
    }
}
