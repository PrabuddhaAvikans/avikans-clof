using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Delivery.Infrastructure.Persistence;

internal sealed class DeliveryConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(DeliveryConfigurationAssembly).Assembly;
}
