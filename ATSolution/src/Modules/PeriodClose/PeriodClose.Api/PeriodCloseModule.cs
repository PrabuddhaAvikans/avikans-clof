using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PeriodClose.Application;
using PeriodClose.Application.Mappings;
using PeriodClose.Infrastructure;

namespace PeriodClose.Api;

public sealed class PeriodCloseModule : IModule
{
    public string Name => ModuleNames.PeriodClose;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(PeriodCloseMappingProfile).Assembly);

        services.AddPeriodCloseApplication()
            .AddPeriodCloseInfrastructure();
    }
}
