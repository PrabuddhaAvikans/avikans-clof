using ATSolution.SharedKernel.Models;

namespace Delivery.Application.Deliveries;

public sealed class DeliveryListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Priority { get; set; }
    public Guid? DriverId { get; set; }
}
