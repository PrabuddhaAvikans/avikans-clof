using Sales.Application.Quotations;
using Sales.Application.SalesOrders;

namespace Sales.Application.Abstractions;

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
