using ATSolution.SharedKernel.Modularity;
using Identity.Api;

namespace ATSolution.Api.Extensions;

internal static class HostModuleExtensions
{
    public static IServiceCollection AddHostModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddModules(
            configuration,
            typeof(IdentityModule).Assembly);

        services.AddControllers()
            .AddApplicationPart(typeof(IdentityModule).Assembly);

        return services;
    }
}