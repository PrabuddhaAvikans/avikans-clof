using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Customers.Infrastructure.Persistence;

internal sealed class CustomersConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(CustomersConfigurationAssembly).Assembly;
}
