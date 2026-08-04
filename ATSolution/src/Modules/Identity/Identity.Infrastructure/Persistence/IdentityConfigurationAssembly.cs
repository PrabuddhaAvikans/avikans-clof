using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Identity.Infrastructure.Persistence;

internal sealed class IdentityConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(IdentityConfigurationAssembly).Assembly;
}