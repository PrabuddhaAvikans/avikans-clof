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
        if (!_configuration.GetValue("Seeding:SeedMockData", false))
        {
            _logger.LogInformation("Commercial mock seeding skipped (Seeding:SeedMockData is false).");
            return;
        }

        var force = _configuration.GetValue("Seeding:ForceReseed", false);

        using var scope = _scopeFactory.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ICommercialDataSeeder>();

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
