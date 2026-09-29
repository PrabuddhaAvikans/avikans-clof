using ATSolution.SharedKernel.Models;
using Finance.Application.Invoices;

namespace Finance.Application.Abstractions;

public interface IInvoiceService
{
    Task<PaginatedResponse<InvoiceDto>> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateAsync(CreateInvoiceCommand command, CancellationToken cancellationToken = default);
    Task<InvoiceDto> UpdateAsync(UpdateInvoiceCommand command, CancellationToken cancellationToken = default);
    Task<InvoiceDto> IssueAsync(IssueInvoiceCommand command, CancellationToken cancellationToken = default);
    Task<InvoiceDto> VoidAsync(VoidInvoiceCommand command, CancellationToken cancellationToken = default);
    Task<InvoiceDto> RecordPaymentAsync(RecordInvoicePaymentCommand command, CancellationToken cancellationToken = default);
}
