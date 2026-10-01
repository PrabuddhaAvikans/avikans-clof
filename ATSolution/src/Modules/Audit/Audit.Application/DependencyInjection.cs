using ATSolution.Application.Abstractions.Audit;
using Audit.Application.Abstractions;
using Audit.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditService>();
        services.AddScoped<IAuditService>(sp => sp.GetRequiredService<AuditService>());
        services.AddScoped<IAuditEventWriter>(sp => sp.GetRequiredService<AuditService>());
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);
        return services;
    }
}
