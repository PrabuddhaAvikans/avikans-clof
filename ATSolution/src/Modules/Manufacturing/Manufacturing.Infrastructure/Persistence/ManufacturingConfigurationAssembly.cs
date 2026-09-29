using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Manufacturing.Infrastructure.Persistence;

internal sealed class ManufacturingConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(ManufacturingConfigurationAssembly).Assembly;
}
