using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Inventory.Infrastructure.Persistence;

internal sealed class InventoryConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(InventoryConfigurationAssembly).Assembly;
}
