using ATSolution.Application.Abstractions.Persistence;
using Configuration.Application.Abstractions;
using Configuration.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Configuration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.ConfigurationConfigurationAssembly>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        return services;
    }
}
