using Catalog.Domain.Products;
using ATSolution.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Sales.Application.Services;

internal readonly record struct CatalogCostSnapshot(
    string? SpecificationsJson,
    string? BomJson,
    string? CostBreakdownJson,
    decimal CostPrice);

internal static class ProductCostCatalog
{
    public static async Task<Func<Guid?, Guid?, CatalogCostSnapshot?>> LoadAsync(
        IRepository<Product, Guid> products,
        IEnumerable<Guid?> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return (_, _) => null;
        }

        var found = await products.Query()
            .AsNoTracking()
            .Include(product => product.Versions)
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);
        var byId = found.ToDictionary(product => product.Id);

        return (productId, versionId) =>
        {
            if (productId is null || !byId.TryGetValue(productId.Value, out var product))
            {
                return null;
            }

            var version = product.Versions.FirstOrDefault(item => versionId.HasValue && item.Id == versionId.Value)
                ?? product.Versions.FirstOrDefault(item => product.CurrentVersionId.HasValue && item.Id == product.CurrentVersionId.Value)
                ?? product.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();
            if (version is null)
            {
                return null;
            }

            return new CatalogCostSnapshot(
                version.SpecificationsJson,
                version.BomJson,
                version.CostBreakdownJson,
                version.CostPrice);
        };
    }
}
