using ATSolution.SharedKernel.Models;

namespace Customers.Application.Customers;

public sealed class CustomerListQuery : PaginatedRequest
{
    public string? Type { get; set; }
    public string? Status { get; set; }
}
