using System.Text;
using ATSolution.Api.Exceptions;
using ATSolution.SharedKernel.Modularity;
using Audit.Api;
using Catalog.Api;
using Configuration.Api;
using Customers.Api;
using Delivery.Api;
using Finance.Api;
using Identity.Api;
using Inventory.Api;
using Manufacturing.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Notifications.Api;
using PeriodClose.Api;
using Reporting.Api;
using Sales.Api;

namespace ATSolution.Api.Extensions;

internal static class HostModuleExtensions
{
    public static IServiceCollection AddHostModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddModules(
            configuration,
            typeof(IdentityModule).Assembly,
            typeof(CatalogModule).Assembly,
            typeof(InventoryModule).Assembly,
            typeof(CustomersModule).Assembly,
            typeof(SalesModule).Assembly,
            typeof(ManufacturingModule).Assembly,
            typeof(DeliveryModule).Assembly,
            typeof(FinanceModule).Assembly,
            typeof(PeriodCloseModule).Assembly,
            typeof(ReportingModule).Assembly,
            typeof(AuditModule).Assembly,
            typeof(ConfigurationModule).Assembly,
            typeof(NotificationsModule).Assembly);

        services.AddSingleton<ApplicationExceptionFilter>();
        services.AddControllers(options =>
                options.Filters.Add<ApplicationExceptionFilter>())
            .AddApplicationPart(typeof(IdentityModule).Assembly)
            .AddApplicationPart(typeof(CatalogModule).Assembly)
            .AddApplicationPart(typeof(InventoryModule).Assembly)
            .AddApplicationPart(typeof(CustomersModule).Assembly)
            .AddApplicationPart(typeof(SalesModule).Assembly)
            .AddApplicationPart(typeof(ManufacturingModule).Assembly)
            .AddApplicationPart(typeof(DeliveryModule).Assembly)
            .AddApplicationPart(typeof(FinanceModule).Assembly)
            .AddApplicationPart(typeof(PeriodCloseModule).Assembly)
            .AddApplicationPart(typeof(ReportingModule).Assembly)
            .AddApplicationPart(typeof(AuditModule).Assembly)
            .AddApplicationPart(typeof(ConfigurationModule).Assembly)
            .AddApplicationPart(typeof(NotificationsModule).Assembly);

        AddJwtAuthentication(services, configuration);
        AddCors(services, configuration);

        return services;
    }

    private static void AddJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (origins.Length == 0)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }
}
