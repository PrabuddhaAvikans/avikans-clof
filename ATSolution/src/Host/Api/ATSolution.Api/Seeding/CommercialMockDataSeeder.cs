using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ATSolution.Infrastructure.Persistence.Data;
using ATSolution.Domain.Entities.Common;
using ATSolution.SharedKernel.Constants;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Customers.Domain.Customers;
using Delivery.Domain.Deliveries;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;
using Identity.Application.Abstractions;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Inventory.Domain.Units;
using Inventory.Domain.Warehouses;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using CatalogDi = Catalog.Infrastructure.DependencyInjection;
using CustomersDi = Customers.Infrastructure.DependencyInjection;
using DeliveryDi = Delivery.Infrastructure.DependencyInjection;
using IdentityDi = Identity.Infrastructure.DependencyInjection;
using InventoryDi = Inventory.Infrastructure.DependencyInjection;
using ManufacturingDi = Manufacturing.Infrastructure.DependencyInjection;
using SalesDi = Sales.Infrastructure.DependencyInjection;
using DeliveryDocumentSequence = Delivery.Domain.Sequences.DocumentSequence;
using ManufacturingDocumentSequence = Manufacturing.Domain.Sequences.DocumentSequence;
using SalesDocumentSequence = Sales.Domain.Sequences.DocumentSequence;
using CatalogStatuses = Catalog.Domain.Common.EntityStatuses;
using DeliveryStatuses = Delivery.Domain.Common.DeliveryStatuses;
using ManufacturingJobStatuses = Manufacturing.Domain.Common.ManufacturingJobStatuses;
using ManufacturingTaskStatuses = Manufacturing.Domain.Common.ManufacturingTaskStatuses;
using TaskUnitAssignmentStatuses = Manufacturing.Domain.Common.TaskUnitAssignmentStatuses;

namespace ATSolution.Api.Seeding;

public sealed class CommercialMockDataSeeder : ICommercialDataSeeder
{
    private const string DemoPassword = "Avikans@123";
    private const string IndoorSlug = "indoor";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly Regex MockIdRegex = new(
        @"^(cat|brd|cus|prd|inv|usr|rol|rg|quo|so|cr|mj|del|qli|sli|cli|tsk|sm|iph|att|qatt|qrev|qch|di|pod|bom|attr|img|cp)-",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExtendedMockIdRegex = new(
        @"^(prd-\d+-ver-\d+|prd-\d+-op-\d+|tsk-[a-z0-9-]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, Assembly> SeedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["roles.json"] = typeof(IdentityDi).Assembly,
        ["role-groups.json"] = typeof(IdentityDi).Assembly,
        ["users.json"] = typeof(IdentityDi).Assembly,
        ["categories.json"] = typeof(CatalogDi).Assembly,
        ["brands.json"] = typeof(CatalogDi).Assembly,
        ["products.json"] = typeof(CatalogDi).Assembly,
        ["inventory.json"] = typeof(InventoryDi).Assembly,
        ["inventory-price-history.json"] = typeof(InventoryDi).Assembly,
        ["stock-movements.json"] = typeof(InventoryDi).Assembly,
        ["customers.json"] = typeof(CustomersDi).Assembly,
        ["quotations.json"] = typeof(SalesDi).Assembly,
        ["sales-orders.json"] = typeof(SalesDi).Assembly,
        ["costing.json"] = typeof(SalesDi).Assembly,
        ["manufacturing.json"] = typeof(ManufacturingDi).Assembly,
        ["deliveries.json"] = typeof(DeliveryDi).Assembly,
    };

    private readonly SqlDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommercialMockDataSeeder> _logger;

    public CommercialMockDataSeeder(
        SqlDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<CommercialMockDataSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SeedStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var isEnabled = _configuration.GetValue("Seeding:SeedMockData", false)
            || _configuration.GetValue("Seeding:AllowManualSeed", true);

        var isSeeded = await _dbContext.Set<Category>()
            .AnyAsync(c => c.Slug == IndoorSlug, cancellationToken);

        return new SeedStatusDto(
            IsEnabled: isEnabled,
            IsSeeded: isSeeded,
            Marker: IndoorSlug,
            Categories: await _dbContext.Set<Category>().CountAsync(cancellationToken),
            Brands: await _dbContext.Set<Brand>().CountAsync(cancellationToken),
            Products: await _dbContext.Set<Product>().CountAsync(cancellationToken),
            Customers: await _dbContext.Set<Customer>().CountAsync(cancellationToken),
            InventoryItems: await _dbContext.Set<InventoryItem>().CountAsync(cancellationToken),
            Quotations: await _dbContext.Set<Quotation>().CountAsync(cancellationToken),
            SalesOrders: await _dbContext.Set<SalesOrder>().CountAsync(cancellationToken),
            CostingRequests: await _dbContext.Set<CostingRequest>().CountAsync(cancellationToken),
            ManufacturingJobs: await _dbContext.Set<ManufacturingJob>().CountAsync(cancellationToken),
            Deliveries: await _dbContext.Set<DeliveryEntity>().CountAsync(cancellationToken),
            Users: await _dbContext.Set<User>().CountAsync(cancellationToken),
            Roles: await _dbContext.Set<Role>().CountAsync(cancellationToken));
    }

    public async Task<SeedResultDto> SeedAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        if (status.IsSeeded && !force)
        {
            var skippedMessage =
                $"Commercial mock seeding skipped — catalog category slug '{IndoorSlug}' already exists.";
            _logger.LogInformation("{Message}", skippedMessage);
            return new SeedResultDto(
                Ran: false,
                Skipped: true,
                Message: skippedMessage,
                Status: status);
        }

        _logger.LogInformation("Seeding commercial mock data from module SeedData resources.");

        var roleMap = await SeedIdentityAsync(_dbContext, _passwordHasher, cancellationToken);
        await SeedWarehousesAndUnitsAsync(_dbContext, cancellationToken);
        await SeedCategoriesAsync(_dbContext, cancellationToken);
        await SeedBrandsAsync(_dbContext, cancellationToken);
        await SeedInventoryAsync(_dbContext, cancellationToken);
        await SeedCustomersAsync(_dbContext, cancellationToken);
        await SeedProductsAsync(_dbContext, cancellationToken);
        await SeedQuotationsAsync(_dbContext, cancellationToken);
        await SeedSalesOrdersAsync(_dbContext, cancellationToken);
        await SeedCostingAsync(_dbContext, cancellationToken);
        await SeedManufacturingAsync(_dbContext, cancellationToken);
        await SeedDeliveriesAsync(_dbContext, cancellationToken);
        await BumpDocumentSequencesAsync(_dbContext, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        status = await GetStatusAsync(cancellationToken);
        var message =
            $"Commercial mock data seeding completed. Roles mapped: {roleMap.Count}. Demo password: {DemoPassword}.";
        _logger.LogInformation("{Message}", message);

        return new SeedResultDto(
            Ran: true,
            Skipped: false,
            Message: message,
            Status: status);
    }

    private Task<Stream> OpenSeedStreamAsync(string fileName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!SeedAssemblies.TryGetValue(fileName, out var assembly))
        {
            throw new InvalidOperationException(
                $"No module SeedData assembly is registered for '{fileName}'.");
        }

        var resourceName = $"SeedData.{fileName}";
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            return Task.FromResult(stream);
        }

        throw new FileNotFoundException(
            $"Seed data file '{fileName}' was not found as embedded resource '{resourceName}' in assembly '{assembly.GetName().Name}'.");
    }

    private async Task<List<T>> LoadAsync<T>(string fileName, CancellationToken ct)
    {
        await using var stream = await OpenSeedStreamAsync(fileName, ct);
        var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct);
        return items ?? [];
    }

    private async Task<JsonDocument> LoadDocumentAsync(string fileName, CancellationToken ct)
    {
        var stream = await OpenSeedStreamAsync(fileName, ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    #region Identity

    private async Task<Dictionary<string, Guid>> SeedIdentityAsync(
        SqlDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken ct)
    {
        var rolesJson = await LoadAsync<RoleSeedDto>("roles.json", ct);
        var groupsJson = await LoadAsync<RoleGroupSeedDto>("role-groups.json", ct);
        var usersJson = await LoadAsync<UserSeedDto>("users.json", ct);

        var permissions = await dbContext.Set<Permission>().ToListAsync(ct);
        var permissionsByCode = permissions.ToDictionary(p => p.Code, StringComparer.Ordinal);

        var adminRole = await dbContext.Set<Role>()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == IdentityMessages.AdminRoleName, ct);

        var roleIdByMockId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var dto in rolesJson)
        {
            var isAdministrator = string.Equals(dto.Name, "Administrator", StringComparison.OrdinalIgnoreCase);
            if (isAdministrator && adminRole is not null)
            {
                roleIdByMockId[dto.Id] = adminRole.Id;
                var permissionIds = ResolvePermissionIds(dto.Permissions, permissionsByCode);
                if (permissionIds.Count > 0)
                {
                    adminRole.SetPermissions(permissionIds);
                }

                continue;
            }

            var roleId = SeedIds.ToGuid(dto.Id);
            var existing = await dbContext.Set<Role>()
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId || r.Name == dto.Name, ct);

            Role role;
            if (existing is not null)
            {
                role = existing;
                roleIdByMockId[dto.Id] = role.Id;
            }
            else
            {
                role = Role.Create(dto.Name, dto.Description, NormalizeStatus(dto.Status), dto.IsSystem);
                AssignId(role, roleId);
                await dbContext.Set<Role>().AddAsync(role, ct);
                roleIdByMockId[dto.Id] = roleId;
            }

            role.SetPermissions(ResolvePermissionIds(dto.Permissions, permissionsByCode));
        }

        foreach (var dto in groupsJson)
        {
            var groupId = SeedIds.ToGuid(dto.Id);
            var group = await dbContext.Set<RoleGroup>()
                .Include(g => g.RoleGroupRoles)
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group is null)
            {
                group = RoleGroup.Create(dto.Name, dto.Description, NormalizeStatus(dto.Status));
                AssignId(group, groupId);
                await dbContext.Set<RoleGroup>().AddAsync(group, ct);
            }

            var roleIds = dto.RoleIds
                .Select(id => roleIdByMockId.TryGetValue(id, out var mapped) ? mapped : SeedIds.ToGuid(id))
                .Distinct()
                .ToList();
            group.SetRoles(roleIds);
        }

        var passwordHash = passwordHasher.Hash(DemoPassword);

        foreach (var dto in usersJson)
        {
            var roleId = roleIdByMockId.TryGetValue(dto.RoleId, out var mappedRole)
                ? mappedRole
                : SeedIds.ToGuid(dto.RoleId);

            var existing = await dbContext.Set<User>()
                .Include(u => u.UserRoleGroups)
                .FirstOrDefaultAsync(
                    u => u.Email == dto.Email || u.Id == SeedIds.ToGuid(dto.Id),
                    ct);

            User user;
            if (existing is not null)
            {
                user = existing;
                user.UpdateProfile(
                    dto.FirstName,
                    dto.LastName,
                    dto.Email,
                    roleId,
                    dto.Phone,
                    dto.Department,
                    dto.JobTitle,
                    NormalizeStatus(dto.Status));

                // Keep working admin password; only set hash for non-admin emails that somehow exist without hash update needs
                if (!string.Equals(dto.Email, IdentityMessages.AdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    user.SetPasswordHash(passwordHash);
                }
            }
            else
            {
                user = User.Create(
                    dto.FirstName,
                    dto.LastName,
                    dto.Email,
                    passwordHash,
                    roleId,
                    dto.Phone,
                    dto.Department,
                    dto.JobTitle,
                    NormalizeStatus(dto.Status));
                AssignId(user, SeedIds.ToGuid(dto.Id));
                await dbContext.Set<User>().AddAsync(user, ct);
            }

            var groupIds = (dto.RoleGroupIds ?? [])
                .Select(SeedIds.ToGuid)
                .Distinct()
                .ToList();
            user.SetRoleGroups(groupIds);

            if (dto.LastLoginAt is DateTimeOffset loginAt)
            {
                SetProperty(user, nameof(User.LastLoginAtUtc), loginAt);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return roleIdByMockId;
    }

    private static List<Guid> ResolvePermissionIds(
        IEnumerable<string>? codes,
        IReadOnlyDictionary<string, Permission> permissionsByCode)
    {
        if (codes is null) return [];

        return codes
            .Where(code => permissionsByCode.ContainsKey(code))
            .Select(code => permissionsByCode[code].Id)
            .Distinct()
            .ToList();
    }

    #endregion

    #region Warehouses / Units / Catalog / Inventory

    private async Task SeedWarehousesAndUnitsAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var inventoryDoc = await LoadDocumentAsync("inventory.json", ct);
        var warehouseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Main Warehouse" };
        var units = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in inventoryDoc.RootElement.EnumerateArray())
        {
            if (item.TryGetProperty("warehouse", out var wh) && wh.ValueKind == JsonValueKind.String)
            {
                var name = wh.GetString();
                if (!string.IsNullOrWhiteSpace(name)) warehouseNames.Add(name);
            }

            if (item.TryGetProperty("unit", out var unit) && unit.ValueKind == JsonValueKind.String)
            {
                var code = unit.GetString();
                if (!string.IsNullOrWhiteSpace(code)) units.Add(code);
            }
        }

        var index = 1;
        foreach (var name in warehouseNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var code = name.Equals("Main Warehouse", StringComparison.OrdinalIgnoreCase)
                ? "WH-MAIN"
                : $"WH-{index:D3}";
            var id = SeedIds.ToGuid($"wh-{code.ToLowerInvariant()}");
            if (await dbContext.Set<Warehouse>().AnyAsync(w => w.Id == id || w.Code == code, ct))
            {
                index++;
                continue;
            }

            var warehouse = Warehouse.Create(code, name);
            AssignId(warehouse, id);
            await dbContext.Set<Warehouse>().AddAsync(warehouse, ct);
            index++;
        }

        foreach (var unitCode in units.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var id = SeedIds.ToGuid($"uom-{unitCode.ToLowerInvariant()}");
            if (await dbContext.Set<UnitOfMeasure>().AnyAsync(u => u.Id == id || u.Code == unitCode, ct))
            {
                continue;
            }

            var uom = UnitOfMeasure.Create(unitCode, unitCode);
            AssignId(uom, id);
            await dbContext.Set<UnitOfMeasure>().AddAsync(uom, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedCategoriesAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        var categories = await LoadAsync<CategorySeedDto>("categories.json", ct);
        var parents = categories.Where(c => string.IsNullOrWhiteSpace(c.ParentId)).ToList();
        var children = categories.Where(c => !string.IsNullOrWhiteSpace(c.ParentId)).ToList();

        foreach (var dto in parents.Concat(children))
        {
            var id = SeedIds.ToGuid(dto.Id);
            if (await dbContext.Set<Category>().AnyAsync(c => c.Id == id || c.Slug == dto.Slug, ct))
            {
                continue;
            }

            var category = Category.Create(
                dto.Name,
                dto.Slug,
                dto.Description,
                SeedIds.ToGuidOrNull(dto.ParentId),
                dto.SortOrder,
                NormalizeStatus(dto.Status),
                dto.ImageUrl);
            AssignId(category, id);
            if (dto.CreatedAt is DateTimeOffset created)
            {
                category.CreatedOnUtc = created;
                category.ModifiedOnUtc = dto.UpdatedAt ?? created;
            }

            await dbContext.Set<Category>().AddAsync(category, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedBrandsAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        var brands = await LoadAsync<BrandSeedDto>("brands.json", ct);
        foreach (var dto in brands)
        {
            var id = SeedIds.ToGuid(dto.Id);
            if (await dbContext.Set<Brand>().AnyAsync(b => b.Id == id || b.Slug == dto.Slug, ct))
            {
                continue;
            }

            var brand = Brand.Create(
                dto.Name,
                dto.Slug,
                dto.Description,
                NormalizeStatus(dto.Status),
                dto.LogoUrl,
                dto.Website,
                dto.CountryOfOrigin);
            AssignId(brand, id);
            await dbContext.Set<Brand>().AddAsync(brand, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedInventoryAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        var items = await LoadAsync<InventoryItemSeedDto>("inventory.json", ct);
        foreach (var dto in items)
        {
            var id = SeedIds.ToGuid(dto.Id);
            if (await dbContext.Set<InventoryItem>().AnyAsync(i => i.Id == id || i.Sku == dto.Sku, ct))
            {
                continue;
            }

            var pricingDate = dto.PricingEffectiveDate ?? dto.CreatedAt ?? DateTimeOffset.UtcNow;
            var item = InventoryItem.Create(
                dto.Sku,
                dto.Name,
                dto.Description,
                dto.Category ?? "General",
                dto.ItemType ?? "component",
                dto.Unit,
                dto.Brand,
                dto.Supplier,
                dto.TaxCode,
                dto.QuantityOnHand,
                dto.Warehouse ?? "Main Warehouse",
                dto.Location ?? string.Empty,
                dto.MinStock,
                dto.MaxStock,
                dto.ReorderLevel,
                dto.ReorderQuantity,
                dto.BuyingPrice,
                dto.CostPrice ?? dto.UnitCost,
                dto.PricingMethod ?? "manual",
                dto.MarkupPercent,
                dto.MarkupFixedAmount,
                dto.SellingPrice,
                pricingDate,
                NormalizeStatus(dto.Status));

            AssignId(item, SeedIds.ToGuid(dto.Id));
            SetProperty(item, nameof(InventoryItem.QuantityReserved), dto.QuantityReserved);
            if (dto.LastRestockedAt is DateTimeOffset restocked)
            {
                SetProperty(item, nameof(InventoryItem.LastRestockedAtUtc), restocked);
            }

            // Refresh stock status after reserved qty
            item.UpdateDetails(
                name: null, description: null, category: null, itemType: null, unit: null,
                brand: null, supplier: null, taxCode: null, warehouse: null, location: null,
                minStock: null, maxStock: null, reorderLevel: null, reorderQuantity: null,
                buyingPrice: null, costPrice: null, pricingMethod: null, markupPercent: null,
                markupFixedAmount: null, sellingPrice: null, pricingEffectiveDate: null,
                status: null, quantityOnHand: dto.QuantityOnHand);

            await dbContext.Set<InventoryItem>().AddAsync(item, ct);
        }

        var priceHistory = await LoadAsync<PriceHistorySeedDto>("inventory-price-history.json", ct);
        foreach (var dto in priceHistory)
        {
            var entry = InventoryPriceHistory.Create(
                SeedIds.ToGuid(dto.InventoryItemId),
                dto.BuyingPrice,
                dto.CostPrice,
                dto.SellingPrice,
                dto.PricingMethod ?? "manual",
                dto.MarkupPercent,
                dto.MarkupFixedAmount,
                dto.EffectiveDate ?? DateTimeOffset.UtcNow,
                SeedIds.ToGuid(dto.ChangedBy).ToString(),
                dto.ChangedByName ?? dto.ChangedBy);
            AssignId(entry, SeedIds.ToGuid(dto.Id));
            if (dto.CreatedAt is DateTimeOffset created)
            {
                SetProperty(entry, nameof(InventoryPriceHistory.CreatedOnUtc), created);
            }

            await dbContext.Set<InventoryPriceHistory>().AddAsync(entry, ct);
        }

        var movements = await LoadAsync<StockMovementSeedDto>("stock-movements.json", ct);
        foreach (var dto in movements)
        {
            var referenceId = dto.ReferenceId;
            if (!string.IsNullOrWhiteSpace(referenceId) && LooksLikeMockId(referenceId))
            {
                referenceId = SeedIds.ToGuid(referenceId).ToString();
            }

            var movement = StockMovement.Create(
                SeedIds.ToGuid(dto.InventoryItemId),
                dto.InventoryItemName,
                dto.InventoryItemSku,
                dto.Type,
                dto.Quantity,
                dto.Unit,
                dto.ReferenceType,
                referenceId,
                dto.Notes,
                SeedIds.ToGuid(dto.PerformedBy).ToString(),
                dto.PerformedByName ?? dto.PerformedBy,
                null);
            AssignId(movement, SeedIds.ToGuid(dto.Id));
            if (dto.PerformedAt is DateTimeOffset performedAt)
            {
                SetProperty(movement, nameof(StockMovement.PerformedAtUtc), performedAt);
            }

            await dbContext.Set<StockMovement>().AddAsync(movement, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    #endregion

    #region Customers / Products

    private async Task SeedCustomersAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("customers.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var id = element.GetProperty("id").GetString()!;
            var billing = RewriteAndSerialize(element.GetProperty("billingAddresses"));
            var contacts = element.TryGetProperty("contactPersons", out var contactsEl)
                ? RewriteAndSerialize(contactsEl)
                : "[]";
            string? shipping = null;
            if (element.TryGetProperty("shippingAddresses", out var shipEl)
                && shipEl.ValueKind is JsonValueKind.Array)
            {
                shipping = RewriteAndSerialize(shipEl);
            }

            var customer = Customer.Create(
                element.GetProperty("code").GetString()!,
                element.GetProperty("name").GetString()!,
                element.GetProperty("type").GetString()!,
                element.GetProperty("email").GetString()!,
                element.TryGetProperty("phone", out var phone) ? phone.GetString() ?? string.Empty : string.Empty,
                billing,
                element.TryGetProperty("activeBillingAddressIndex", out var abi) ? abi.GetInt32() : 0,
                !element.TryGetProperty("deliverySameAsBilling", out var dsb) || dsb.GetBoolean(),
                shipping,
                element.TryGetProperty("activeShippingAddressIndex", out var asi) && asi.ValueKind == JsonValueKind.Number
                    ? asi.GetInt32()
                    : null,
                contacts,
                element.TryGetProperty("taxId", out var taxId) ? taxId.GetString() : null,
                element.TryGetProperty("creditLimit", out var credit) && credit.ValueKind == JsonValueKind.Number
                    ? credit.GetDecimal()
                    : null,
                element.TryGetProperty("paymentTermsDays", out var terms) ? terms.GetInt32() : 30,
                element.TryGetProperty("notes", out var notes) ? notes.GetString() : null,
                NormalizeStatus(element.TryGetProperty("status", out var status) ? status.GetString() : CatalogStatuses.Active));

            AssignId(customer, SeedIds.ToGuid(id));
            if (element.TryGetProperty("totalOrders", out var orders) && orders.ValueKind == JsonValueKind.Number)
            {
                SetProperty(customer, nameof(Customer.TotalOrders), orders.GetInt32());
            }

            if (element.TryGetProperty("totalRevenue", out var revenue) && revenue.ValueKind == JsonValueKind.Number)
            {
                SetProperty(customer, nameof(Customer.TotalRevenue), revenue.GetDecimal());
            }

            await dbContext.Set<Customer>().AddAsync(customer, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedProductsAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("products.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var mockId = element.GetProperty("id").GetString()!;
            var productId = SeedIds.ToGuid(mockId);
            var versionId = SeedIds.ToGuid($"{mockId}-ver-1");

            var categoryId = SeedIds.ToGuid(element.GetProperty("categoryId").GetString()!);
            Guid? brandId = element.TryGetProperty("brandId", out var brandEl)
                && brandEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(brandEl.GetString())
                    ? SeedIds.ToGuid(brandEl.GetString()!)
                    : null;

            Guid? customerId = element.TryGetProperty("customerId", out var custEl)
                && custEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(custEl.GetString())
                    ? SeedIds.ToGuid(custEl.GetString()!)
                    : null;

            var customerName = element.TryGetProperty("customerName", out var cn) ? cn.GetString() : null;
            var productType = element.TryGetProperty("productType", out var pt)
                && pt.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(pt.GetString())
                    ? pt.GetString()!
                    : "finished_good";

            var product = Product.Create(
                element.GetProperty("sku").GetString()!,
                element.GetProperty("name").GetString()!,
                element.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                categoryId,
                brandId,
                productType,
                NormalizeStatus(element.TryGetProperty("status", out var st) ? st.GetString() : CatalogStatuses.Active),
                customerId,
                customerName,
                currency: element.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "LKR" : "LKR",
                createdBy: element.TryGetProperty("createdBy", out var cb) && cb.ValueKind == JsonValueKind.String
                    ? SeedIds.ToGuid(cb.GetString()!).ToString()
                    : "system");
            AssignId(product, productId);

            var basePrice = element.TryGetProperty("basePrice", out var bp) ? bp.GetDecimal() : 0m;
            var costPrice = element.TryGetProperty("costPrice", out var cp) ? cp.GetDecimal() : 0m;
            var margin = costPrice <= 0 ? 0 : Math.Round((basePrice - costPrice) / costPrice * 100m, 2);

            var specs = BuildSpecificationsJson(element);
            var bomJson = RewriteAndSerialize(GetPropertyOrDefault(element, "bom"), "[]");
            var opsJson = RewriteAndSerialize(GetPropertyOrDefault(element, "operations"), "[]");
            var attrsJson = RewriteAndSerialize(GetPropertyOrDefault(element, "attributes"), "[]");
            var imagesJson = RewriteAndSerialize(GetPropertyOrDefault(element, "images"), "[]");
            var tagsJson = RewriteAndSerialize(GetPropertyOrDefault(element, "tags"), "[]");

            var version = ProductVersion.Create(
                productId,
                1,
                ProductVersionStatuses.Released,
                specs,
                bomJson,
                opsJson,
                attrsJson,
                imagesJson,
                "{}",
                tagsJson,
                basePrice,
                costPrice,
                margin,
                element.TryGetProperty("leadTimeDays", out var ltd) ? ltd.GetInt32() : 0,
                element.TryGetProperty("minOrderQuantity", out var moq) ? moq.GetInt32() : 1);
            AssignId(version, versionId);

            product.Versions.Add(version);
            product.SetCurrentVersion(versionId);
            await dbContext.Set<Product>().AddAsync(product, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static string BuildSpecificationsJson(JsonElement element)
    {
        decimal? weight = null;
        if (element.TryGetProperty("weightKg", out var w) && w.ValueKind == JsonValueKind.Number)
        {
            weight = w.GetDecimal();
        }

        string? dimensions = null;
        if (element.TryGetProperty("dimensions", out var d) && d.ValueKind == JsonValueKind.String)
        {
            dimensions = d.GetString();
        }

        return JsonSerializer.Serialize(new { weightKg = weight, dimensions }, JsonOptions);
    }

    #endregion

    #region Sales

    private async Task SeedQuotationsAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("quotations.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var mockId = element.GetProperty("id").GetString()!;
            var quotationId = SeedIds.ToGuid(mockId);
            var customerId = SeedIds.ToGuid(element.GetProperty("customerId").GetString()!);
            var createdBy = element.TryGetProperty("createdBy", out var cb)
                ? SeedIds.ToGuid(cb.GetString()!).ToString()
                : "system";
            var createdByName = element.TryGetProperty("createdByName", out var cbn)
                ? cbn.GetString() ?? "System"
                : "System";

            var quotation = Quotation.Create(
                element.GetProperty("quotationNumber").GetString()!,
                customerId,
                element.GetProperty("customerName").GetString()!,
                element.TryGetProperty("customerEmail", out var email) ? email.GetString() ?? string.Empty : string.Empty,
                element.TryGetProperty("status", out var status) ? status.GetString() ?? QuotationStatuses.Draft : QuotationStatuses.Draft,
                element.TryGetProperty("priority", out var priority) ? priority.GetString() ?? PriorityValues.Medium : PriorityValues.Medium,
                element.TryGetProperty("validUntil", out var valid) && valid.ValueKind == JsonValueKind.String
                    ? DateTimeOffset.Parse(valid.GetString()!)
                    : DateTimeOffset.UtcNow.AddDays(30),
                element.TryGetProperty("notes", out var notes) ? notes.GetString() : null,
                element.TryGetProperty("terms", out var terms) ? terms.GetString() : null,
                GetDecimal(element, "discountAmount"),
                GetDecimal(element, "subtotal"),
                GetDecimal(element, "taxAmount"),
                GetDecimal(element, "totalAmount"),
                element.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "LKR" : "LKR",
                element.TryGetProperty("paymentStatus", out var pay) ? pay.GetString() ?? PaymentStatuses.Unpaid : PaymentStatuses.Unpaid,
                RewriteAndSerialize(GetPropertyOrDefault(element, "billingAddress"), "{}"),
                element.TryGetProperty("shippingAddress", out var ship) && ship.ValueKind == JsonValueKind.Object
                    ? RewriteAndSerialize(ship)
                    : null,
                RewriteAndSerialize(GetPropertyOrDefault(element, "attachments"), "[]"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "revisions"), "[]"),
                null,
                createdBy,
                createdByName);
            AssignId(quotation, quotationId);

            if (element.TryGetProperty("sentAt", out var sentAt) && sentAt.ValueKind == JsonValueKind.String)
            {
                SetProperty(quotation, nameof(Quotation.SentAtUtc), DateTimeOffset.Parse(sentAt.GetString()!));
            }

            if (element.TryGetProperty("salesOrderId", out var soId)
                && soId.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(soId.GetString()))
            {
                SetProperty(quotation, nameof(Quotation.SalesOrderId), SeedIds.ToGuid(soId.GetString()!));
            }

            var sort = 0;
            if (element.TryGetProperty("lineItems", out var lines) && lines.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in lines.EnumerateArray())
                {
                    var lineEntity = QuotationLine.Create(
                        quotationId,
                        SeedIds.ToGuidOrNull(GetStringOrNull(line, "productId")),
                        GetStringOrNull(line, "productSku") ?? string.Empty,
                        GetStringOrNull(line, "productName") ?? string.Empty,
                        SeedIds.ToGuidOrNull(GetStringOrNull(line, "productVersionId")),
                        GetStringOrNull(line, "productVersionLabel"),
                        GetStringOrNull(line, "description"),
                        GetDecimal(line, "quantity"),
                        GetDecimal(line, "unitPrice"),
                        GetDecimal(line, "discountPercent"),
                        GetDecimal(line, "taxPercent"),
                        GetDecimal(line, "lineTotal"),
                        line.TryGetProperty("isCustomized", out var ic) && ic.GetBoolean(),
                        line.TryGetProperty("requiresManufacturing", out var rm) && rm.GetBoolean(),
                        line.TryGetProperty("customization", out var cust) ? RewriteAndSerialize(cust) : null,
                        sort++);
                    if (line.TryGetProperty("id", out var lineId) && lineId.ValueKind == JsonValueKind.String)
                    {
                        AssignId(lineEntity, SeedIds.ToGuid(lineId.GetString()!));
                    }

                    quotation.Lines.Add(lineEntity);
                }
            }

            if (element.TryGetProperty("contactHistory", out var contacts) && contacts.ValueKind == JsonValueKind.Array)
            {
                foreach (var contact in contacts.EnumerateArray())
                {
                    var contactEntity = QuotationContact.Create(
                        quotationId,
                        GetStringOrNull(contact, "type") ?? "note",
                        GetStringOrNull(contact, "summary") ?? string.Empty,
                        GetStringOrNull(contact, "detail"),
                        GetStringOrNull(contact, "outcome"),
                        contact.TryGetProperty("contactedBy", out var cby) && !string.IsNullOrWhiteSpace(cby.GetString())
                            ? SeedIds.ToGuid(cby.GetString()!).ToString()
                            : "system",
                        GetStringOrNull(contact, "contactedByName") ?? "System",
                        contact.TryGetProperty("contactedAt", out var cat) && cat.ValueKind == JsonValueKind.String
                            ? DateTimeOffset.Parse(cat.GetString()!)
                            : null);
                    if (contact.TryGetProperty("id", out var contactId) && contactId.ValueKind == JsonValueKind.String)
                    {
                        AssignId(contactEntity, SeedIds.ToGuid(contactId.GetString()!));
                    }

                    quotation.Contacts.Add(contactEntity);
                }
            }

            await dbContext.Set<Quotation>().AddAsync(quotation, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedSalesOrdersAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("sales-orders.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var mockId = element.GetProperty("id").GetString()!;
            var orderId = SeedIds.ToGuid(mockId);
            var createdBy = element.TryGetProperty("createdBy", out var cb)
                ? SeedIds.ToGuid(cb.GetString()!).ToString()
                : "system";
            var createdByName = GetStringOrNull(element, "createdByName") ?? "System";

            var order = SalesOrder.Create(
                element.GetProperty("orderNumber").GetString()!,
                SeedIds.ToGuid(element.GetProperty("customerId").GetString()!),
                element.GetProperty("customerName").GetString()!,
                GetStringOrNull(element, "customerEmail") ?? string.Empty,
                SeedIds.ToGuidOrNull(GetStringOrNull(element, "quotationId")),
                GetStringOrNull(element, "quotationNumber"),
                GetStringOrNull(element, "priority") ?? PriorityValues.Medium,
                GetDecimal(element, "discountAmount"),
                GetDecimal(element, "subtotal"),
                GetDecimal(element, "taxAmount"),
                GetDecimal(element, "totalAmount"),
                GetStringOrNull(element, "currency") ?? "LKR",
                RewriteAndSerialize(GetPropertyOrDefault(element, "billingAddress"), "{}"),
                element.TryGetProperty("shippingAddress", out var ship) && ship.ValueKind == JsonValueKind.Object
                    ? RewriteAndSerialize(ship)
                    : null,
                element.TryGetProperty("requestedDeliveryDate", out var rdd) && rdd.ValueKind == JsonValueKind.String
                    ? DateTimeOffset.Parse(rdd.GetString()!)
                    : null,
                GetStringOrNull(element, "notes"),
                null,
                createdBy,
                createdByName);
            AssignId(order, orderId);

            var status = GetStringOrNull(element, "status") ?? SalesOrderStatuses.Draft;
            order.SetStatus(status);

            if (element.TryGetProperty("paymentStatus", out var pay) && pay.ValueKind == JsonValueKind.String)
            {
                SetProperty(order, nameof(SalesOrder.PaymentStatus), pay.GetString()!);
            }

            if (element.TryGetProperty("assignedTo", out var assigned)
                && assigned.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(assigned.GetString()))
            {
                order.Assign(SeedIds.ToGuid(assigned.GetString()!), GetStringOrNull(element, "assignedToName") ?? string.Empty);
            }

            if (element.TryGetProperty("confirmedAt", out var confirmed) && confirmed.ValueKind == JsonValueKind.String)
            {
                SetProperty(order, nameof(SalesOrder.ConfirmedAtUtc), DateTimeOffset.Parse(confirmed.GetString()!));
            }

            if (element.TryGetProperty("manufacturingJobIds", out var mjIds) && mjIds.ValueKind == JsonValueKind.Array)
            {
                SetProperty(order, nameof(SalesOrder.ManufacturingJobIdsJson), RewriteAndSerialize(mjIds));
            }

            if (element.TryGetProperty("deliveryIds", out var delIds) && delIds.ValueKind == JsonValueKind.Array)
            {
                SetProperty(order, nameof(SalesOrder.DeliveryIdsJson), RewriteAndSerialize(delIds));
            }

            var sort = 0;
            if (element.TryGetProperty("lineItems", out var lines) && lines.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in lines.EnumerateArray())
                {
                    var lineEntity = SalesOrderLine.Create(
                        orderId,
                        SeedIds.ToGuidOrNull(GetStringOrNull(line, "productId")),
                        GetStringOrNull(line, "productSku") ?? string.Empty,
                        GetStringOrNull(line, "productName") ?? string.Empty,
                        SeedIds.ToGuidOrNull(GetStringOrNull(line, "productVersionId")),
                        GetStringOrNull(line, "productVersionLabel"),
                        GetStringOrNull(line, "description"),
                        GetDecimal(line, "quantity"),
                        GetDecimal(line, "unitPrice"),
                        GetDecimal(line, "discountPercent"),
                        GetDecimal(line, "taxPercent"),
                        GetDecimal(line, "lineTotal"),
                        line.TryGetProperty("isCustomized", out var ic) && ic.GetBoolean(),
                        line.TryGetProperty("requiresManufacturing", out var rm) && rm.GetBoolean(),
                        line.TryGetProperty("customization", out var cust) ? RewriteAndSerialize(cust) : null,
                        sort++);
                    if (line.TryGetProperty("id", out var lineId) && lineId.ValueKind == JsonValueKind.String)
                    {
                        AssignId(lineEntity, SeedIds.ToGuid(lineId.GetString()!));
                    }

                    lineEntity.SetQuantityDelivered(GetDecimal(line, "quantityDelivered"));
                    var inMfg = GetDecimal(line, "quantityInManufacturing");
                    if (inMfg > 0)
                    {
                        lineEntity.AllocateToManufacturing(inMfg);
                    }

                    order.Lines.Add(lineEntity);
                }
            }

            await dbContext.Set<SalesOrder>().AddAsync(order, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedCostingAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("costing.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var mockId = element.GetProperty("id").GetString()!;
            var costingId = SeedIds.ToGuid(mockId);

            // Mock costing rows are not bound to seeded SOs; unique synthetic SO ids satisfy the unique index.
            var salesOrderId = element.TryGetProperty("salesOrderId", out var soEl)
                && soEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(soEl.GetString())
                    ? SeedIds.ToGuid(soEl.GetString()!)
                    : SeedIds.ToGuid($"costing-so-{mockId}");

            var salesOrderNumber = GetStringOrNull(element, "salesOrderNumber") ?? "N/A";
            var quotationId = SeedIds.ToGuidOrNull(GetStringOrNull(element, "quotationId"));

            var request = CostingRequest.Create(
                element.GetProperty("requestNumber").GetString()!,
                salesOrderId,
                salesOrderNumber,
                quotationId,
                GetStringOrNull(element, "quotationNumber"),
                GetStringOrNull(element, "customerName") ?? string.Empty,
                GetStringOrNull(element, "projectName") ?? string.Empty,
                GetDecimal(element, "totalEstimate"),
                GetDecimal(element, "proposedPrice"),
                GetDecimal(element, "marginPercent"),
                GetStringOrNull(element, "currency") ?? "LKR",
                GetStringOrNull(element, "paymentTerms") ?? "Net 30",
                RewriteAndSerialize(GetPropertyOrDefault(element, "lineItems"), "[]"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "coatingItems"), "[]"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "estimationMaterials"), "[]"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "estimationProductLines"), "[]"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "requester"), "{}"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "approvalLevels"), "[]"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "history"), "[]"),
                GetStringOrNull(element, "status") ?? CostingRequestStatuses.Pending,
                GetStringOrNull(element, "coatingStatus") ?? CoatingStatuses.Pending,
                null,
                GetStringOrNull(element, "notes"));

            AssignId(request, costingId);
            SetProperty(request, nameof(CostingRequest.AttachmentsJson),
                RewriteAndSerialize(GetPropertyOrDefault(element, "attachments"), "[]"));
            SetProperty(request, nameof(CostingRequest.TargetMargin), GetDecimal(element, "targetMargin", 25));
            SetProperty(request, nameof(CostingRequest.RiskFlag), GetStringOrNull(element, "riskFlag") ?? "medium");
            SetProperty(request, nameof(CostingRequest.SlaRemaining), GetStringOrNull(element, "slaRemaining") ?? "48h");
            SetProperty(request, nameof(CostingRequest.RequestType),
                GetStringOrNull(element, "requestType") ?? "Custom Fabrication");

            if (element.TryGetProperty("requestedDate", out var rd) && rd.ValueKind == JsonValueKind.String)
            {
                SetProperty(request, nameof(CostingRequest.RequestedDateUtc), DateTimeOffset.Parse(rd.GetString()!));
            }

            await dbContext.Set<CostingRequest>().AddAsync(request, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    #endregion

    #region Manufacturing / Delivery / Sequences

    private async Task SeedManufacturingAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("manufacturing.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var mockId = element.GetProperty("id").GetString()!;
            var jobId = SeedIds.ToGuid(mockId);
            var productMockId = GetStringOrNull(element, "productId");
            var productVersionId = SeedIds.ToGuidOrNull(GetStringOrNull(element, "productVersionId"))
                ?? (productMockId is not null ? SeedIds.ToGuid($"{productMockId}-ver-1") : null);

            var plannedStart = element.TryGetProperty("plannedStartDate", out var ps) && ps.ValueKind == JsonValueKind.String
                ? DateTimeOffset.Parse(ps.GetString()!)
                : DateTimeOffset.UtcNow;
            var plannedEnd = element.TryGetProperty("plannedEndDate", out var pe) && pe.ValueKind == JsonValueKind.String
                ? DateTimeOffset.Parse(pe.GetString()!)
                : plannedStart.AddDays(14);

            var job = ManufacturingJob.Create(
                element.GetProperty("jobNumber").GetString()!,
                SeedIds.ToGuid(element.GetProperty("salesOrderId").GetString()!),
                GetStringOrNull(element, "salesOrderNumber") ?? string.Empty,
                SeedIds.ToGuid(element.GetProperty("customerId").GetString()!),
                GetStringOrNull(element, "customerName") ?? string.Empty,
                SeedIds.ToGuid(productMockId ?? "prd-unknown"),
                GetStringOrNull(element, "productSku") ?? string.Empty,
                GetStringOrNull(element, "productName") ?? string.Empty,
                productVersionId,
                GetStringOrNull(element, "productVersionLabel") ?? "V1",
                GetDecimal(element, "quantity"),
                GetStringOrNull(element, "priority") ?? PriorityValues.Medium,
                plannedStart,
                plannedEnd,
                SeedIds.ToGuidOrNull(GetStringOrNull(element, "assignedTo")),
                GetStringOrNull(element, "assignedToName"),
                GetStringOrNull(element, "notes"),
                RewriteAndSerialize(GetPropertyOrDefault(element, "materialRequirements"), "[]"),
                element.TryGetProperty("createdBy", out var cb) && cb.ValueKind == JsonValueKind.String
                    ? SeedIds.ToGuid(cb.GetString()!).ToString()
                    : "system",
                GetStringOrNull(element, "createdByName") ?? "System");
            AssignId(job, jobId);

            var jobStatus = GetStringOrNull(element, "status") ?? ManufacturingJobStatuses.Draft;
            job.SetStatus(jobStatus);
            job.SetOverallProgress(GetDecimal(element, "progressPercent"));
            job.SetCosts(GetDecimal(element, "estimatedCost"), GetDecimal(element, "actualCost"));
            job.SetReworksJson(RewriteAndSerialize(GetPropertyOrDefault(element, "reworks"), "[]"));

            if (element.TryGetProperty("actualStartDate", out var asd) && asd.ValueKind == JsonValueKind.String)
            {
                SetProperty(job, nameof(ManufacturingJob.ActualStartUtc), DateTimeOffset.Parse(asd.GetString()!));
            }

            if (element.TryGetProperty("actualEndDate", out var aed) && aed.ValueKind == JsonValueKind.String)
            {
                SetProperty(job, nameof(ManufacturingJob.ActualEndUtc), DateTimeOffset.Parse(aed.GetString()!));
            }

            var taskSeeds = element.TryGetProperty("taskSeeds", out var seeds) && seeds.ValueKind == JsonValueKind.Array
                ? seeds
                : element.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array
                    ? tasks
                    : default;

            if (taskSeeds.ValueKind == JsonValueKind.Array)
            {
                foreach (var taskEl in taskSeeds.EnumerateArray())
                {
                    SeedManufacturingTask(job, jobId, taskEl);
                }
            }

            await dbContext.Set<ManufacturingJob>().AddAsync(job, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static void SeedManufacturingTask(ManufacturingJob job, Guid jobId, JsonElement taskEl)
    {
        var taskMockId = GetStringOrNull(taskEl, "id") ?? Guid.NewGuid().ToString("N");
        var taskId = SeedIds.ToGuid(taskMockId);
        var plannedQty = (int)Math.Max(1, Math.Round(GetDecimal(taskEl, "plannedQuantity", GetDecimal(taskEl, "quantity", job.Quantity))));

        var prereqIds = new List<Guid>();
        if (taskEl.TryGetProperty("prerequisiteTaskIds", out var prereqs) && prereqs.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in prereqs.EnumerateArray())
            {
                if (p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString()))
                {
                    prereqIds.Add(SeedIds.ToGuid(p.GetString()!));
                }
            }
        }

        var sequenceNo = taskEl.TryGetProperty("sequence", out var sequenceEl) ? sequenceEl.GetInt32() : 0;
        var task = ManufacturingTask.Create(
            jobId,
            GetStringOrNull(taskEl, "taskNumber") ?? $"T-{sequenceNo}",
            GetStringOrNull(taskEl, "name") ?? "Task",
            sequenceNo,
            GetDecimal(taskEl, "estimatedHours"),
            plannedQty,
            !taskEl.TryGetProperty("isRequired", out var req) || req.ValueKind != JsonValueKind.False,
            !taskEl.TryGetProperty("isEnabled", out var en) || en.ValueKind != JsonValueKind.False,
            taskEl.TryGetProperty("isQcTask", out var qc) && qc.ValueKind == JsonValueKind.True,
            SeedIds.ToGuidOrNull(GetStringOrNull(taskEl, "productOperationId")),
            GetStringOrNull(taskEl, "description"),
            taskEl.TryGetProperty("labourCostRate", out var lcr) && lcr.ValueKind == JsonValueKind.Number
                ? lcr.GetDecimal()
                : null,
            GetStringOrNull(taskEl, "machineName"),
            taskEl.TryGetProperty("machineCost", out var mc) && mc.ValueKind == JsonValueKind.Number
                ? mc.GetDecimal()
                : null,
            RewriteAndSerialize(taskEl),
            prereqIds);

        AssignId(task, taskId);
        foreach (var unit in task.Units)
        {
            // Re-bind TaskId after AssignId; TaskUnit.Create already used task.Id before overwrite
            SetProperty(unit, nameof(TaskUnit.TaskId), taskId);
        }

        var status = GetStringOrNull(taskEl, "status") ?? ManufacturingTaskStatuses.Pending;
        var now = DateTimeOffset.UtcNow;
        if (taskEl.TryGetProperty("startedAt", out var started) && started.ValueKind == JsonValueKind.String)
        {
            now = DateTimeOffset.Parse(started.GetString()!);
        }

        task.SetStatus(status, now);
        task.SetActualHours(GetDecimal(taskEl, "actualHours"));
        task.SetAssignees(
            SeedIds.ToGuidOrNull(GetStringOrNull(taskEl, "assignedTo")),
            GetStringOrNull(taskEl, "assignedToName"));

        if (taskEl.TryGetProperty("completedAt", out var completed) && completed.ValueKind == JsonValueKind.String)
        {
            SetProperty(task, nameof(ManufacturingTask.CompletedAtUtc), DateTimeOffset.Parse(completed.GetString()!));
        }

        var completedQty = (int)Math.Round(GetDecimal(taskEl, "completedQuantity"));
        for (var i = 0; i < task.Units.Count; i++)
        {
            var unit = task.Units.ElementAt(i);
            if (i < completedQty || status == ManufacturingTaskStatuses.Completed)
            {
                unit.SetProgress(100);
            }
            else if (status == ManufacturingTaskStatuses.InProgress && i == completedQty)
            {
                unit.SetProgress(50);
            }
        }

        if (taskEl.TryGetProperty("contributors", out var contributors) && contributors.ValueKind == JsonValueKind.Array)
        {
            var unitList = task.Units.ToList();
            var contributorIndex = 0;
            foreach (var contributor in contributors.EnumerateArray())
            {
                var userId = SeedIds.ToGuid(GetStringOrNull(contributor, "userId") ?? "usr-unknown");
                var userName = GetStringOrNull(contributor, "userName") ?? "Worker";
                var contribStatus = GetStringOrNull(contributor, "status") ?? TaskUnitAssignmentStatuses.Assigned;
                var startedAt = contributor.TryGetProperty("startedAt", out var sa) && sa.ValueKind == JsonValueKind.String
                    ? DateTimeOffset.Parse(sa.GetString()!)
                    : (DateTimeOffset?)null;

                var targetUnit = unitList[contributorIndex % unitList.Count];
                contributorIndex++;

                var assignment = TaskUnitAssignment.Create(
                    targetUnit.Id,
                    userId,
                    userName,
                    GetDecimal(contributor, "contributionPercent", 100),
                    contribStatus,
                    startedAt);
                assignment.ApplyProgress(
                    GetDecimal(contributor, "contributionPercent", 100),
                    contribStatus,
                    GetDecimal(contributor, "actualHours"),
                    GetDecimal(contributor, "normalOvertimeHours"),
                    GetDecimal(contributor, "doubleOvertimeHours"),
                    GetDecimal(contributor, "laborCost"),
                    GetDecimal(contributor, "rejectedQuantity"),
                    GetDecimal(contributor, "wasteQuantity"),
                    startedAt ?? DateTimeOffset.UtcNow);
                targetUnit.Assignments.Add(assignment);
                targetUnit.SyncStatusFromProgress();
            }
        }

        job.Tasks.Add(task);
    }

    private async Task SeedDeliveriesAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        using var doc = await LoadDocumentAsync("deliveries.json", ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var mockId = element.GetProperty("id").GetString()!;
            var deliveryId = SeedIds.ToGuid(mockId);
            var itemsJson = RewriteAndSerialize(GetPropertyOrDefault(element, "items"), "[]");

            var delivery = DeliveryEntity.Create(
                element.GetProperty("deliveryNumber").GetString()!,
                SeedIds.ToGuid(element.GetProperty("salesOrderId").GetString()!),
                GetStringOrNull(element, "salesOrderNumber") ?? string.Empty,
                SeedIds.ToGuid(element.GetProperty("customerId").GetString()!),
                GetStringOrNull(element, "customerName") ?? string.Empty,
                GetStringOrNull(element, "priority") ?? PriorityValues.Medium,
                element.TryGetProperty("scheduledDate", out var sd) && sd.ValueKind == JsonValueKind.String
                    ? DateTimeOffset.Parse(sd.GetString()!)
                    : DateTimeOffset.UtcNow,
                itemsJson,
                element.TryGetProperty("shippingAddress", out var ship) && ship.ValueKind == JsonValueKind.Object
                    ? RewriteAndSerialize(ship)
                    : null,
                SeedIds.ToGuidOrNull(GetStringOrNull(element, "driverId")),
                GetStringOrNull(element, "driverName"),
                GetStringOrNull(element, "vehicleNumber") ?? GetStringOrNull(element, "vehicle"),
                GetStringOrNull(element, "carrier"),
                GetStringOrNull(element, "notes"),
                element.TryGetProperty("createdBy", out var cb) && cb.ValueKind == JsonValueKind.String
                    ? SeedIds.ToGuid(cb.GetString()!).ToString()
                    : "system",
                GetStringOrNull(element, "createdByName") ?? "System");

            AssignId(delivery, deliveryId);

            if (element.TryGetProperty("trackingNumber", out var tn) && tn.ValueKind == JsonValueKind.String)
            {
                SetProperty(delivery, nameof(DeliveryEntity.TrackingNumber), tn.GetString());
            }

            // Bypass transition rules for historical seed statuses.
            var status = GetStringOrNull(element, "status") ?? DeliveryStatuses.Planned;
            SetProperty(delivery, nameof(DeliveryEntity.Status), status);

            if (element.TryGetProperty("dispatchedAt", out var disp) && disp.ValueKind == JsonValueKind.String)
            {
                SetProperty(delivery, nameof(DeliveryEntity.DispatchedAtUtc), DateTimeOffset.Parse(disp.GetString()!));
            }

            if (element.TryGetProperty("deliveredAt", out var delAt) && delAt.ValueKind == JsonValueKind.String)
            {
                SetProperty(delivery, nameof(DeliveryEntity.DeliveredAtUtc), DateTimeOffset.Parse(delAt.GetString()!));
            }

            if (element.TryGetProperty("proofOfDelivery", out var pod) && pod.ValueKind == JsonValueKind.Object)
            {
                SetProperty(delivery, nameof(DeliveryEntity.ProofOfDeliveryJson), RewriteAndSerialize(pod));
            }

            await dbContext.Set<DeliveryEntity>().AddAsync(delivery, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task BumpDocumentSequencesAsync(
        SqlDbContext dbContext,
        CancellationToken ct)
    {
        static IEnumerable<(int Year, int Number)> ParseNumbers(JsonDocument doc, string numberProperty, params string[] prefixes)
        {
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (!element.TryGetProperty(numberProperty, out var numEl) || numEl.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var number = numEl.GetString() ?? string.Empty;
                foreach (var prefix in prefixes)
                {
                    var match = Regex.Match(
                        number,
                        $@"^{Regex.Escape(prefix)}-(\d{{4}})-(\d+)$",
                        RegexOptions.IgnoreCase);
                    if (match.Success
                        && int.TryParse(match.Groups[1].Value, out var year)
                        && int.TryParse(match.Groups[2].Value, out var n))
                    {
                        yield return (year, n);
                    }
                }
            }
        }

        async Task EnsureSalesSequence(string documentType, string fileName, string numberProperty, string[] prefixes)
        {
            using var doc = await LoadDocumentAsync(fileName, ct);
            foreach (var group in ParseNumbers(doc, numberProperty, prefixes).GroupBy(x => x.Year))
            {
                var max = group.Max(x => x.Number);
                if (max <= 0) continue;

                var year = group.Key;
                var sequence = await dbContext.Set<SalesDocumentSequence>()
                    .FirstOrDefaultAsync(x => x.DocumentType == documentType && x.Year == year, ct);
                if (sequence is null)
                {
                    sequence = SalesDocumentSequence.Create(documentType, year, max);
                    await dbContext.Set<SalesDocumentSequence>().AddAsync(sequence, ct);
                }
                else
                {
                    while (sequence.LastNumber < max)
                    {
                        sequence.Next();
                    }
                }
            }
        }

        await EnsureSalesSequence(DocumentSequenceTypes.Quotation, "quotations.json", "quotationNumber", ["Q", "QT"]);
        await EnsureSalesSequence(DocumentSequenceTypes.SalesOrder, "sales-orders.json", "orderNumber", ["SO"]);
        await EnsureSalesSequence(DocumentSequenceTypes.CostingRequest, "costing.json", "requestNumber", ["CR"]);

        // Delivery sequence
        {
            using var doc = await LoadDocumentAsync("deliveries.json", ct);
            foreach (var group in ParseNumbers(doc, "deliveryNumber", "DL").GroupBy(x => x.Year))
            {
                var max = group.Max(x => x.Number);
                if (max <= 0) continue;
                var year = group.Key;
                var sequence = await dbContext.Set<DeliveryDocumentSequence>()
                    .FirstOrDefaultAsync(x => x.DocumentType == DocumentSequenceTypes.Delivery && x.Year == year, ct);
                if (sequence is null)
                {
                    sequence = DeliveryDocumentSequence.Create(DocumentSequenceTypes.Delivery, year);
                    await dbContext.Set<DeliveryDocumentSequence>().AddAsync(sequence, ct);
                }

                while (sequence.LastNumber < max)
                {
                    sequence.Next();
                }
            }
        }

        // Manufacturing uses MJ-year-####; mock uses PJ-#### — bump current-year MJ from max PJ/MJ.
        {
            using var doc = await LoadDocumentAsync("manufacturing.json", ct);
            var byYear = new Dictionary<int, int>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var number = GetStringOrNull(element, "jobNumber") ?? string.Empty;
                var mj = Regex.Match(number, @"^MJ-(\d{4})-(\d+)$", RegexOptions.IgnoreCase);
                if (mj.Success
                    && int.TryParse(mj.Groups[1].Value, out var year)
                    && int.TryParse(mj.Groups[2].Value, out var n1))
                {
                    byYear[year] = Math.Max(byYear.GetValueOrDefault(year), n1);
                    continue;
                }

                var pj = Regex.Match(number, @"^PJ-(\d+)$", RegexOptions.IgnoreCase);
                if (pj.Success && int.TryParse(pj.Groups[1].Value, out var n2))
                {
                    // PJ numbers have no year; park them under the current UTC year as a floor for new MJ docs.
                    var y = DateTimeOffset.UtcNow.Year;
                    byYear[y] = Math.Max(byYear.GetValueOrDefault(y), n2);
                }
            }

            foreach (var (year, max) in byYear)
            {
                if (max <= 0) continue;
                var sequence = await dbContext.Set<ManufacturingDocumentSequence>()
                    .FirstOrDefaultAsync(
                        x => x.DocumentType == DocumentSequenceTypes.ManufacturingJob && x.Year == year,
                        ct);
                if (sequence is null)
                {
                    sequence = ManufacturingDocumentSequence.Create(DocumentSequenceTypes.ManufacturingJob, year);
                    await dbContext.Set<ManufacturingDocumentSequence>().AddAsync(sequence, ct);
                }

                while (sequence.LastNumber < max)
                {
                    sequence.Next();
                }
            }
        }
    }

    #endregion

    #region Helpers

    private static void AssignId<TEntity>(TEntity entity, Guid id)
        where TEntity : Entity<Guid>
    {
        var property = typeof(Entity<Guid>).GetProperty(nameof(Entity<Guid>.Id))
            ?? throw new InvalidOperationException("Entity.Id property not found.");
        property.SetValue(entity, id);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var type = target.GetType();
        while (type is not null)
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property is not null)
            {
                property.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }

        throw new InvalidOperationException($"Property '{propertyName}' not found on {target.GetType().Name}.");
    }

    private static bool LooksLikeMockId(string value) =>
        MockIdRegex.IsMatch(value) || ExtendedMockIdRegex.IsMatch(value);

    private static string RewriteAndSerialize(JsonElement element, string fallback = "null")
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return fallback;
        }

        var node = JsonNode.Parse(element.GetRawText());
        RewriteMockIds(node);
        return node?.ToJsonString(JsonOptions) ?? fallback;
    }

    private static void RewriteMockIds(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (property.Value is JsonValue value
                        && value.TryGetValue<string>(out var text)
                        && !string.IsNullOrWhiteSpace(text)
                        && LooksLikeMockId(text))
                    {
                        obj[property.Key] = SeedIds.ToGuid(text).ToString();
                    }
                    else
                    {
                        RewriteMockIds(property.Value);
                    }
                }

                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] is JsonValue value
                        && value.TryGetValue<string>(out var text)
                        && !string.IsNullOrWhiteSpace(text)
                        && LooksLikeMockId(text))
                    {
                        array[i] = SeedIds.ToGuid(text).ToString();
                    }
                    else
                    {
                        RewriteMockIds(array[i]);
                    }
                }

                break;
        }
    }

    private static JsonElement GetPropertyOrDefault(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value : default;

    private static string? GetStringOrNull(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal GetDecimal(JsonElement element, string name, decimal fallback = 0m) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : fallback;

    private static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? CatalogStatuses.Active : status.Trim().ToLowerInvariant();

    #endregion

    #region DTOs

    private sealed class RoleSeedDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = [];
        public bool IsSystem { get; set; }
        public string? Status { get; set; }
    }

    private sealed class RoleGroupSeedDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public List<string> RoleIds { get; set; } = [];
        public string? Status { get; set; }
    }

    private sealed class UserSeedDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public string RoleId { get; set; } = null!;
        public List<string>? RoleGroupIds { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
    }

    private sealed class CategorySeedDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public int SortOrder { get; set; }
        public string? Status { get; set; }
        public string? ImageUrl { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    private sealed class BrandSeedDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public string? CountryOfOrigin { get; set; }
        public string? Status { get; set; }
    }

    private sealed class InventoryItemSeedDto
    {
        public string Id { get; set; } = null!;
        public string Sku { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? ItemType { get; set; }
        public string Unit { get; set; } = "pcs";
        public string? Brand { get; set; }
        public string? Supplier { get; set; }
        public string? TaxCode { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public string? Warehouse { get; set; }
        public string? Location { get; set; }
        public decimal MinStock { get; set; }
        public decimal MaxStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal ReorderQuantity { get; set; }
        public decimal? BuyingPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal? CostPrice { get; set; }
        public string? PricingMethod { get; set; }
        public decimal MarkupPercent { get; set; }
        public decimal MarkupFixedAmount { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTimeOffset? PricingEffectiveDate { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? LastRestockedAt { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }

    private sealed class PriceHistorySeedDto
    {
        public string Id { get; set; } = null!;
        public string InventoryItemId { get; set; } = null!;
        public decimal? BuyingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string? PricingMethod { get; set; }
        public decimal MarkupPercent { get; set; }
        public decimal MarkupFixedAmount { get; set; }
        public DateTimeOffset? EffectiveDate { get; set; }
        public string ChangedBy { get; set; } = "usr-001";
        public string? ChangedByName { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }

    private sealed class StockMovementSeedDto
    {
        public string Id { get; set; } = null!;
        public string InventoryItemId { get; set; } = null!;
        public string InventoryItemName { get; set; } = null!;
        public string InventoryItemSku { get; set; } = null!;
        public string Type { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "pcs";
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public string? Notes { get; set; }
        public string PerformedBy { get; set; } = "usr-001";
        public string? PerformedByName { get; set; }
        public DateTimeOffset? PerformedAt { get; set; }
    }

    #endregion
}
