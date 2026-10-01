using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Quotations;

public sealed class QuotationListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Priority { get; set; }
}
