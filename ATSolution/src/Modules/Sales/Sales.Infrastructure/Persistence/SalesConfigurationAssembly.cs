using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Sales.Infrastructure.Persistence;

internal sealed class SalesConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(SalesConfigurationAssembly).Assembly;
}
