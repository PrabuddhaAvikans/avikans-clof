using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Abstractions;
using Sales.Infrastructure.Persistence.Repositories;

namespace Sales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.SalesConfigurationAssembly>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        return services;
    }
}
