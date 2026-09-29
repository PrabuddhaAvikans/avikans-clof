using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Application;
using Reporting.Application.Mappings;
using Reporting.Infrastructure;

namespace Reporting.Api;

public sealed class ReportingModule : IModule
{
    public string Name => ModuleNames.Reporting;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(ReportingMappingProfile).Assembly);

        services.AddReportingApplication()
            .AddReportingInfrastructure();
    }
}
