using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.SalesOrders;

public sealed class SalesOrderListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Priority { get; set; }
    public Guid? AssignedTo { get; set; }
}

public sealed record SalesOrderLineDto(
    Guid Id,
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    string? Description,
    Guid? ProductVersionId,
    string? ProductVersionLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    decimal QuantityDelivered,
    decimal QuantityInManufacturing,
    bool IsCustomized,
    JsonElement? Customization,
    bool RequiresManufacturing);

public sealed record SalesOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid? QuotationId,
    string? QuotationNumber,
    Guid? CostingRequestId,
    string Status,
    string Priority,
    IReadOnlyList<SalesOrderLineDto> LineItems,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string PaymentStatus,
    Quotations.AddressDto BillingAddress,
    Quotations.AddressDto? ShippingAddress,
    DateTimeOffset? RequestedDeliveryDate,
    string? Notes,
    Guid? AssignedTo,
    string? AssignedToName,
    IReadOnlyList<string> ManufacturingJobIds,
    IReadOnlyList<string> DeliveryIds,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SalesOrderLineInputDto(
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    string? Description,
    Guid? ProductVersionId,
    string? ProductVersionLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    bool? IsCustomized,
    JsonElement? Customization,
    bool? RequiresManufacturing);

public sealed record CreateSalesOrderCommand(
    Guid CustomerId,
    Guid? QuotationId,
    string? QuotationNumber,
    IReadOnlyList<SalesOrderLineInputDto> LineItems,
    string Priority,
    DateTimeOffset? RequestedDeliveryDate,
    string? Notes,
    decimal? DiscountAmount,
    string? CreatedBy,
    string? CreatedByName,
    JsonElement? WorkflowSnapshot);

public sealed record UpdateSalesOrderCommand(
    Guid Id,
    Guid? CustomerId = null,
    Guid? QuotationId = null,
    string? QuotationNumber = null,
    IReadOnlyList<SalesOrderLineInputDto>? LineItems = null,
    string? Priority = null,
    DateTimeOffset? RequestedDeliveryDate = null,
    string? Notes = null,
    decimal? DiscountAmount = null);

public sealed record CancelSalesOrderCommand(Guid Id, string? Reason);
public sealed record AssignSalesOrderCommand(Guid Id, Guid UserId);
