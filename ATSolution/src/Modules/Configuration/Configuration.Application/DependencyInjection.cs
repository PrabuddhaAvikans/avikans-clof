using Configuration.Application.Abstractions;
using Configuration.Application.Services;
using ATSolution.Application.Abstractions.Workflows;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Configuration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationApplication(this IServiceCollection services)
    {
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<ICostingApprovalFlowProvider, CostingApprovalFlowProvider>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);
        return services;
    }
}
