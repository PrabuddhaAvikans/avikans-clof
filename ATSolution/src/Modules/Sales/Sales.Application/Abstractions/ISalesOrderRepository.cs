using Customers.Domain.Customers;
using Identity.Domain.Users;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Sales.Application.SalesOrders;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;

namespace Sales.Application.Abstractions;

public interface ISalesOrderRepository
{
    Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> SearchAsync(
        SalesOrderListQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SalesOrder?> GetWithLinesAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken = default);

    Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Quotation?> FindQuotationWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddOrderAsync(SalesOrder order, CancellationToken cancellationToken = default);

    Task<string> NextNumberAsync(string documentType, CancellationToken cancellationToken = default);

    Task AddCostingAsync(CostingRequest costing, CancellationToken cancellationToken = default);

    void RemoveLines(IEnumerable<SalesOrderLine> lines);

    Task<CostingRequest?> FindCostingByOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);

    void RemoveCosting(CostingRequest costing);

    void RemoveOrder(SalesOrder order);

    Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryItem?> FindInventoryItemAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
}
