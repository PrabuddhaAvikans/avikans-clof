using ATSolution.SharedKernel.Models;

namespace Finance.Application.CreditNotes;

public sealed class CreditNoteListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
}
