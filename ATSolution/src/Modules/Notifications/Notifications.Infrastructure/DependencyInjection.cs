using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, NotificationsConfigurationAssembly>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }
}
