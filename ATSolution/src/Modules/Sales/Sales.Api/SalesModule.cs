using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application;
using Sales.Application.Mappings;
using Sales.Infrastructure;

namespace Sales.Api;

public sealed class SalesModule : IModule
{
    public string Name => ModuleNames.Sales;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(SalesMappingProfile).Assembly);

        services.AddSalesApplication()
            .AddSalesInfrastructure();
    }
}
