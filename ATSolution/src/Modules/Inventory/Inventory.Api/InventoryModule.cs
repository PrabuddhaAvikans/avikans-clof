using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Inventory.Application;
using Inventory.Application.Mappings;
using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Api;

public sealed class InventoryModule : IModule
{
    public string Name => ModuleNames.Inventory;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(InventoryMappingProfile).Assembly);

        services.AddInventoryApplication()
            .AddInventoryInfrastructure();
    }
}
