using System.Text.Json;
namespace Sales.Application.Costing;

public sealed record CoatingSubmitItemDto(
    Guid? Id,
    Guid? ProductId,
    string ProductName,
    string Finish,
    string Process,
    decimal Quantity,
    decimal UnitCost,
    Guid? SalesOrderLineItemId = null,
    string? SourceType = null,
    string? ProductVersionLabel = null,
    string? ProductSku = null);
