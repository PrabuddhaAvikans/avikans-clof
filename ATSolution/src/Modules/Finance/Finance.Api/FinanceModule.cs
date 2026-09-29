using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Finance.Application;
using Finance.Application.Mappings;
using Finance.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Api;

public sealed class FinanceModule : IModule
{
    public string Name => ModuleNames.Finance;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(FinanceMappingProfile).Assembly);

        services.AddFinanceApplication()
            .AddFinanceInfrastructure();
    }
}
