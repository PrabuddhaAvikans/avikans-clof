using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Infrastructure.Persistence;
using ATSolution.Infrastructure.Persistence.Data;
using ATSolution.Infrastructure.Validation;
using ATSolution.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATSolution.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfigurationManager configuration)
    {
        services.AddDbContext<SqlDbContext>(options =>
           options.UseSqlServer(
               configuration.GetConnectionString(ConnectionStringNames.SqlConnectionString),
               options => options.EnableRetryOnFailure()));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<SqlDbContext>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IApplicationValidator, ApplicationValidator>();

        return services;
    }
}
