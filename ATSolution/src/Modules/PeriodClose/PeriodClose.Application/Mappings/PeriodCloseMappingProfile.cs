using AutoMapper;
using PeriodClose.Application.PeriodClose;
using PeriodClose.Domain.Periods;
using PeriodClose.Domain.Settings;

namespace PeriodClose.Application.Mappings;

public sealed class PeriodCloseMappingProfile : Profile
{
    public PeriodCloseMappingProfile()
    {
        CreateMap<BusinessPeriod, BusinessPeriodDto>();
        CreateMap<MonthlyPeriod, MonthlyPeriodDto>();
        CreateMap<PeriodCloseSettings, PeriodCloseSettingsDto>();
    }
}
