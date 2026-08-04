using ATSolution.Application.Abstractions.Persistence;
using Identity.Application.Abstractions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
       this IServiceCollection services)
    {
        services.AddSingleton<
            IEntityConfigurationAssembly,
            IdentityConfigurationAssembly>();

        services.AddScoped<IIdentityRepository, IdentityRepository>();

        return services;
    }
}