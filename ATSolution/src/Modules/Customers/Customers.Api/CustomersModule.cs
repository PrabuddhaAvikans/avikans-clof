using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Customers.Application;
using Customers.Application.Mappings;
using Customers.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Api;

public sealed class CustomersModule : IModule
{
    public string Name => ModuleNames.Customers;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(CustomersMappingProfile).Assembly);

        services.AddCustomersApplication()
            .AddCustomersInfrastructure();
    }
}
