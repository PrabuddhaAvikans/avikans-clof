using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Catalog.Application;
using Catalog.Application.Mappings;
using Catalog.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Api;

public sealed class CatalogModule : IModule
{
    public string Name => ModuleNames.Catalog;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(CatalogMappingProfile).Assembly);

        services.AddCatalogApplication()
            .AddCatalogInfrastructure();
    }
}
