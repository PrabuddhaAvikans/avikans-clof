using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application;
using Notifications.Application.Mappings;
using Notifications.Infrastructure;

namespace Notifications.Api;

public sealed class NotificationsModule : IModule
{
    public string Name => ModuleNames.Notifications;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(NotificationsMappingProfile).Assembly);

        services.AddNotificationsApplication()
            .AddNotificationsInfrastructure();
    }
}
