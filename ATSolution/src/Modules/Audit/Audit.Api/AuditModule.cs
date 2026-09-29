using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Audit.Application;
using Audit.Application.Mappings;
using Audit.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Api;

public sealed class AuditModule : IModule
{
    public string Name => ModuleNames.Audit;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(AuditMappingProfile).Assembly);

        services.AddAuditApplication()
            .AddAuditInfrastructure();
    }
}
