using Delivery.Application.Abstractions;
using Delivery.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Delivery.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDeliveryApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);
        return services;
    }
}
