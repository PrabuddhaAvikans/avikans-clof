using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace Finance.Infrastructure.Persistence;

internal sealed class FinanceConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(FinanceConfigurationAssembly).Assembly;
}
