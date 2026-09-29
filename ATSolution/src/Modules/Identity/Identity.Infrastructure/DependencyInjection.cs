using ATSolution.Application.Abstractions.Persistence;
using Identity.Application.Abstractions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Seeding;
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

        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddHostedService<IdentityDataSeeder>();

        return services;
    }
}
