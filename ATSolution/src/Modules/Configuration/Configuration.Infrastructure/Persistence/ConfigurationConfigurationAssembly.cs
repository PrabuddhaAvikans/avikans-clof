using System.Reflection;
using ATSolution.Application.Abstractions.Persistence;

namespace Configuration.Infrastructure.Persistence;

internal sealed class ConfigurationConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(ConfigurationConfigurationAssembly).Assembly;
}
