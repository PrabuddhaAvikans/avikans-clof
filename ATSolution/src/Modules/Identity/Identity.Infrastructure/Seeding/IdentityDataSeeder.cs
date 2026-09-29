using ATSolution.Infrastructure.Persistence.Data;
using ATSolution.SharedKernel.Constants;
using Identity.Application.Abstractions;
using Identity.Application.Permissions;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Seeding;

public sealed class IdentityDataSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        IServiceScopeFactory scopeFactory,
        ILogger<IdentityDataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        try
        {
            await SeedAsync(dbContext, passwordHasher, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Identity data seeding failed. Apply EF migrations, then restart the API.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAsync(
        SqlDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var permissionsByCode = await EnsurePermissionsAsync(dbContext, cancellationToken);
        var adminRole = await EnsureAdminRoleAsync(dbContext, permissionsByCode, cancellationToken);
        await EnsureAdminUserAsync(dbContext, passwordHasher, adminRole.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Identity data seeding completed.");
    }

    private static async Task<Dictionary<string, Permission>> EnsurePermissionsAsync(
        SqlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<Permission>()
            .ToListAsync(cancellationToken);

        var byCode = existing.ToDictionary(permission => permission.Code, StringComparer.Ordinal);

        foreach (var (module, action, code) in IdentityPermissionCatalog.All)
        {
            if (byCode.ContainsKey(code))
            {
                continue;
            }

            var permission = Permission.Create(module, action);
            await dbContext.Set<Permission>().AddAsync(permission, cancellationToken);
            byCode[code] = permission;
        }

        return byCode;
    }

    private static async Task<Role> EnsureAdminRoleAsync(
        SqlDbContext dbContext,
        IReadOnlyDictionary<string, Permission> permissionsByCode,
        CancellationToken cancellationToken)
    {
        var adminRole = await dbContext.Set<Role>()
            .Include(role => role.RolePermissions)
            .FirstOrDefaultAsync(
                role => role.Name == IdentityMessages.AdminRoleName,
                cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create(
                IdentityMessages.AdminRoleName,
                "System administrator with all permissions.",
                EntityStatuses.Active,
                isSystem: true);
            await dbContext.Set<Role>().AddAsync(adminRole, cancellationToken);
        }

        adminRole.SetPermissions(permissionsByCode.Values.Select(permission => permission.Id));
        return adminRole;
    }

    private static async Task EnsureAdminUserAsync(
        SqlDbContext dbContext,
        IPasswordHasher passwordHasher,
        Guid adminRoleId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Set<User>()
            .AnyAsync(user => user.Email == IdentityMessages.AdminEmail, cancellationToken);

        if (exists)
        {
            return;
        }

        var adminUser = User.Create(
            "Prabuddha",
            "Admin",
            IdentityMessages.AdminEmail,
            passwordHasher.Hash(IdentityMessages.AdminPassword),
            adminRoleId,
            status: EntityStatuses.Active);

        await dbContext.Set<User>().AddAsync(adminUser, cancellationToken);
    }
}
