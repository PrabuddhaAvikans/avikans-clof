using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Costing;

public sealed class CostingListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public string? CoatingStatus { get; set; }
    public Guid? SalesOrderId { get; set; }
    public bool? LinkedToSalesOrder { get; set; }
}
