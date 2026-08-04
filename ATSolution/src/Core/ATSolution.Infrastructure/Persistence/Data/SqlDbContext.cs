using ATSolution.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATSolution.Infrastructure.Persistence.Data;

public class SqlDbContext : DbContext, IApplicationDbContext
{
    private readonly IEnumerable<IEntityConfigurationAssembly>
       _configurationAssemblies;

    public SqlDbContext(DbContextOptions<SqlDbContext> options,
        IEnumerable<IEntityConfigurationAssembly> configurationAssemblies) : base(options)
    {
        _configurationAssemblies = configurationAssemblies;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Core entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SqlDbContext).Assembly);

        // Module entity configurations
        foreach (var configurationAssembly in _configurationAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(configurationAssembly.Assembly);
        }
    }
}