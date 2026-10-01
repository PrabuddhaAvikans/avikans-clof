using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record ProductAttributeDto(Guid Id, string Name, string Value, string? Unit);
