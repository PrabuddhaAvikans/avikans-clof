using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record CostSheetLineDto(Guid Id, string Handle, decimal Amount);
