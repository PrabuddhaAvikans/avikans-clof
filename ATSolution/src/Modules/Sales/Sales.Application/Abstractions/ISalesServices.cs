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

public interface ISalesOrderService
{
    Task<ATSolution.SharedKernel.Models.PaginatedResponse<SalesOrderDto>> ListAsync(
        SalesOrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<SalesOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderCommand command, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> UpdateAsync(UpdateSalesOrderCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CancelAsync(CancelSalesOrderCommand command, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> AssignAsync(AssignSalesOrderCommand command, CancellationToken cancellationToken = default);
}

public interface ICostingService
{
    Task<ATSolution.SharedKernel.Models.PaginatedResponse<Costing.CostingRequestDto>> ListAsync(
        Costing.CostingListQuery query,
        CancellationToken cancellationToken = default);

    Task<Costing.CostingRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto?> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> CreateFromSalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> SyncFromSalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> SubmitCoatingAsync(Costing.SubmitCoatingCommand command, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> ApproveAsync(Costing.CostingDecisionCommand command, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> RejectAsync(Costing.CostingDecisionCommand command, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> RequestChangesAsync(Costing.CostingDecisionCommand command, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> UpdateNotesAsync(Costing.UpdateCostingNotesCommand command, CancellationToken cancellationToken = default);
    Task<Costing.CostingRequestDto> AddCommentAsync(Costing.CostingCommentCommand command, CancellationToken cancellationToken = default);
}
