using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.ReportingConfigurationAssembly>();
        return services;
    }
}
