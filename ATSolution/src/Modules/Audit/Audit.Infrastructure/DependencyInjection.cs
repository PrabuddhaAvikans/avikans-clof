using ATSolution.Application.Abstractions.Persistence;
using Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.AuditConfigurationAssembly>();
        services.AddScoped<ISaveChangesInterceptor, ActivityAuditSaveChangesInterceptor>();
        return services;
    }
}
