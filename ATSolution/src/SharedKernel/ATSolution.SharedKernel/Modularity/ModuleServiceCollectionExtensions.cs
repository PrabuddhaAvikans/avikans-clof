using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ATSolution.SharedKernel.Modularity;

public static class ModuleServiceCollectionExtensions
{
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var moduleTypes = assembly.GetExportedTypes()
                .Where(type => typeof(IModule).IsAssignableFrom(type)
                    && type is { IsAbstract: false, IsInterface: false });

            foreach (var moduleType in moduleTypes)
            {
                if (Activator.CreateInstance(moduleType) is IModule module)
                {
                    module.RegisterServices(services, configuration);
                }
            }
        }

        return services;
    }
}