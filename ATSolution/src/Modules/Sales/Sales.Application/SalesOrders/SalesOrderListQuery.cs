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
