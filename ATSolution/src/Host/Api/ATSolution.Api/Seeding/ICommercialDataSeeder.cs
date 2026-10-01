namespace ATSolution.Api.Seeding;

public interface ICommercialDataSeeder
{
    Task<SeedStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds commercial mock data from each module's embedded SeedData JSON.
    /// When <paramref name="force"/> is false, skips if already seeded.
    /// </summary>
    Task<SeedResultDto> SeedAsync(bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds audit logs when the table is empty. Returns true if rows were inserted.
    /// </summary>
    Task<bool> EnsureAuditLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds the costing approval workflow from mock roles and users when steps are still unassigned.
    /// Returns true if a catalog row was created or updated.
    /// </summary>
    Task<bool> EnsureWorkflowCatalogAsync(CancellationToken cancellationToken = default);
}

public sealed record SeedStatusDto(
    bool IsEnabled,
    bool IsSeeded,
    string Marker,
    int Categories,
    int Brands,
    int Products,
    int Customers,
    int InventoryItems,
    int Quotations,
    int SalesOrders,
    int CostingRequests,
    int ManufacturingJobs,
    int Deliveries,
    int Users,
    int Roles);

public sealed record SeedResultDto(
    bool Ran,
    bool Skipped,
    string Message,
    SeedStatusDto Status);
