using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Manufacturing.Application.Jobs;

public sealed class ManufacturingListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? Priority { get; set; }
}
