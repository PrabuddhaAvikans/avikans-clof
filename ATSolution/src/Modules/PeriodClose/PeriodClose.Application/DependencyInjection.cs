using PeriodClose.Application.Abstractions;
using PeriodClose.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PeriodClose.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPeriodCloseApplication(this IServiceCollection services)
    {
        services.AddScoped<IPeriodCloseService, PeriodCloseService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);
        return services;
    }
}
