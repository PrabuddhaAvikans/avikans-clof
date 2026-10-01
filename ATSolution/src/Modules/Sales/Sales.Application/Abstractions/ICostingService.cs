using Sales.Application.Quotations;
using Sales.Application.SalesOrders;

namespace Sales.Application.Abstractions;

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
