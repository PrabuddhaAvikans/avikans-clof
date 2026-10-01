using System.Text.Json.Nodes;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.SalesOrders;

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
    JsonNode? WorkflowSnapshot = null);
