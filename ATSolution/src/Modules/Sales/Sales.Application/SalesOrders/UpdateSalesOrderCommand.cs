using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.SalesOrders;

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
