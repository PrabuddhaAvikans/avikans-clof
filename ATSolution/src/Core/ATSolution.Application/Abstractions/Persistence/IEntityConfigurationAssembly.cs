using System.Reflection;

namespace ATSolution.Application.Abstractions.Persistence;

public interface IEntityConfigurationAssembly
{
    Assembly Assembly { get; }
}