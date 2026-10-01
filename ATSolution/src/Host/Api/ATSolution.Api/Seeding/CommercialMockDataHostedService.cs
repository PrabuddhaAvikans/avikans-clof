namespace ATSolution.Api.Seeding;

/// <summary>
/// Startup hook that runs commercial mock seeding when Seeding:SeedMockData is true.
/// Actual work lives in <see cref="ICommercialDataSeeder"/>.
/// </summary>
public sealed class CommercialMockDataHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommercialMockDataHostedService> _logger;

    public CommercialMockDataHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CommercialMockDataHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ICommercialDataSeeder>();

        if (!_configuration.GetValue("Seeding:SeedMockData", false))
        {
            try
            {
                var auditSeeded = await seeder.EnsureAuditLogsAsync(cancellationToken);
                var workflowsSeeded = await seeder.EnsureWorkflowCatalogAsync(cancellationToken);
                if (auditSeeded)
                {
                    _logger.LogInformation("Audit logs were seeded because the table was empty.");
                }

                if (workflowsSeeded)
                {
                    _logger.LogInformation("Costing approval workflow was seeded from mock roles and users.");
                }

                if (!auditSeeded && !workflowsSeeded)
                {
                    _logger.LogInformation("Commercial mock seeding skipped (Seeding:SeedMockData is false).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log ensure-on-startup failed.");
            }

            return;
        }

        var force = _configuration.GetValue("Seeding:ForceReseed", false);

        try
        {
            var result = await seeder.SeedAsync(force, cancellationToken);
            _logger.LogInformation("{Message}", result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Commercial mock data seeding failed. Apply EF migrations, then restart the API.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
