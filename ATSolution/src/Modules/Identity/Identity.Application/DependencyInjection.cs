using Identity.Application.Abstractions;
using Identity.Application.Security;
using Identity.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}