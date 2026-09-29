using System.Reflection;
using ATSolution.Application.Abstractions.Persistence;

namespace Audit.Infrastructure.Persistence;

internal sealed class AuditConfigurationAssembly : IEntityConfigurationAssembly
{
    public Assembly Assembly => typeof(AuditConfigurationAssembly).Assembly;
}
