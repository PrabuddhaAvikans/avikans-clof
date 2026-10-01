using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using PeriodClose.Application.Abstractions;
using PeriodClose.Infrastructure.Services;

namespace PeriodClose.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPeriodCloseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.PeriodCloseConfigurationAssembly>();
        services.AddScoped<IPeriodCloseOperations, EfPeriodCloseOperations>();
        return services;
    }
}
