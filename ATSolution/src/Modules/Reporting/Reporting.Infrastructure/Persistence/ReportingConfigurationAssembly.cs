using System.Reflection;
using ATSolution.Application.Abstractions.Persistence;

namespace Reporting.Infrastructure.Persistence;

internal sealed class ReportingConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(ReportingConfigurationAssembly).Assembly;
}
