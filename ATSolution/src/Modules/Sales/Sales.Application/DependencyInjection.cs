using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Abstractions;
using Sales.Application.Services;
using FluentValidation;

namespace Sales.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<ICostingService, CostingService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);
        return services;
    }
}
