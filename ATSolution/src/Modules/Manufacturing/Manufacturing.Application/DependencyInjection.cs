using FluentValidation;
using Manufacturing.Application.Abstractions;
using Manufacturing.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Manufacturing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddManufacturingApplication(this IServiceCollection services)
    {
        services.AddScoped<IManufacturingService, ManufacturingService>();
        services.AddScoped<IProductionTrackingService, ProductionTrackingService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);
        return services;
    }
}
