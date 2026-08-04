using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Identity.Api.Mappings;
using Identity.Application;
using Identity.Application.Mappings;
using Identity.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Api;

public sealed class IdentityModule : IModule
{
    public string Name => ModuleNames.Identity;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(UserApiMappingProfile).Assembly,
            typeof(UserMappingProfile).Assembly);

        services.AddIdentityApplication()
                .AddIdentityInfrastructure();
    }
}