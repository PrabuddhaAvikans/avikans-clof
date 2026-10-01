using Sales.Application.Quotations;
using Sales.Application.SalesOrders;

namespace Sales.Application.Abstractions;

public interface IQuotationService
{
    Task<ATSolution.SharedKernel.Models.PaginatedResponse<QuotationDto>> ListAsync(
        QuotationListQuery query,
        CancellationToken cancellationToken = default);

    Task<QuotationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuotationDto> CreateAsync(CreateQuotationCommand command, CancellationToken cancellationToken = default);
    Task<QuotationDto> UpdateAsync(UpdateQuotationCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuotationDto> SendAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> ConvertToSalesOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuotationDto> AddContactEntryAsync(AddQuotationContactCommand command, CancellationToken cancellationToken = default);
    Task<QuotationDto> ApproveLineCustomizationAsync(
        Guid quotationId,
        Guid lineItemId,
        string? notes,
        CancellationToken cancellationToken = default);
    Task<PromoteCustomizationResultDto> PromoteCustomizationToProductVersionAsync(
        Guid quotationId,
        Guid lineItemId,
        string? revisionNotes,
        CancellationToken cancellationToken = default);
}
