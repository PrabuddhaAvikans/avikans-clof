using ATSolution.Application.Abstractions.Persistence;
using System.Reflection;

namespace PeriodClose.Infrastructure.Persistence;

internal sealed class PeriodCloseConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(PeriodCloseConfigurationAssembly).Assembly;
}
