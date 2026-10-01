using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record ProductAccessoryDto(Guid Id, string Handle, string Name, string? Sku, decimal? Quantity);
