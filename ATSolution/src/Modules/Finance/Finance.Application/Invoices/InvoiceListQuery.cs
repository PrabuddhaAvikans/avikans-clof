using ATSolution.SharedKernel.Models;

namespace Finance.Application.Invoices;

public sealed class InvoiceListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
}
