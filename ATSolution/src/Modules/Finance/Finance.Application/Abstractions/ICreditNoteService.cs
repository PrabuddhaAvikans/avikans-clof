using ATSolution.SharedKernel.Models;
using Finance.Application.CreditNotes;

namespace Finance.Application.Abstractions;

public interface ICreditNoteService
{
    Task<PaginatedResponse<CreditNoteDto>> ListAsync(CreditNoteListQuery query, CancellationToken cancellationToken = default);
    Task<CreditNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreditNoteDto> CreateAsync(CreateCreditNoteCommand command, CancellationToken cancellationToken = default);
    Task<CreditNoteDto> UpdateAsync(UpdateCreditNoteCommand command, CancellationToken cancellationToken = default);
    Task<CreditNoteDto> IssueAsync(IssueCreditNoteCommand command, CancellationToken cancellationToken = default);
    Task<CreditNoteDto> VoidAsync(VoidCreditNoteCommand command, CancellationToken cancellationToken = default);
    Task<CreditNoteDto> ApplyToInvoiceAsync(ApplyCreditNoteCommand command, CancellationToken cancellationToken = default);
}
