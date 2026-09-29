using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Catalog.Infrastructure.Persistence;

internal sealed class CatalogConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(CatalogConfigurationAssembly).Assembly;
}
