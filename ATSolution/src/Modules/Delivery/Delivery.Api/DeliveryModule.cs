using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Delivery.Application;
using Delivery.Application.Mappings;
using Delivery.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delivery.Api;

public sealed class DeliveryModule : IModule
{
    public string Name => ModuleNames.Delivery;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(DeliveryMappingProfile).Assembly);

        services.AddDeliveryApplication()
            .AddDeliveryInfrastructure();
    }
}
