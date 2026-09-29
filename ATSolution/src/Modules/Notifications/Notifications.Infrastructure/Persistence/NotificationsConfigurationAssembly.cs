using System.Reflection;
using ATSolution.Application.Abstractions.Persistence;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationsConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(NotificationsConfigurationAssembly).Assembly;
}
