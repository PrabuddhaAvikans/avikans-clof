using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record ProductImageDto(Guid Id, string Url, string? Alt, bool IsPrimary, int SortOrder);
