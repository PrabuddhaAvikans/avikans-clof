using Customers.Domain.Customers;
using Identity.Domain.Users;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.SalesOrders;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using Sales.Domain.Sequences;
using SqlDbContext = ATSolution.Infrastructure.Persistence.Data.SqlDbContext;

namespace Sales.Infrastructure.Persistence.Repositories;

internal sealed class SalesOrderRepository : ISalesOrderRepository
{
    private readonly SqlDbContext _context;

    public SalesOrderRepository(SqlDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> SearchAsync(
        SalesOrderListQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = _context.Set<SalesOrder>()
            .AsNoTracking()
            .Include(order => order.Lines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            items = items.Where(order => order.Status == query.Status);
        }

        if (query.CustomerId.HasValue)
        {
            items = items.Where(order => order.CustomerId == query.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            items = items.Where(order => order.Priority == query.Priority);
        }

        if (query.AssignedTo.HasValue)
        {
            items = items.Where(order => order.AssignedToUserId == query.AssignedTo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(order =>
                order.Number.Contains(search)
                || order.CustomerName.Contains(search)
                || order.CustomerEmail.Contains(search));
        }

        items = items.OrderByDescending(order => order.ModifiedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (pageItems, totalCount);
    }

    public Task<SalesOrder?> GetWithLinesAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<SalesOrder>().Include(order => order.Lines).AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<Customer>().FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public Task<Quotation?> FindQuotationWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<Quotation>()
            .Include(quotation => quotation.Lines)
            .FirstOrDefaultAsync(quotation => quotation.Id == id, cancellationToken);
    }

    public async Task AddOrderAsync(SalesOrder order, CancellationToken cancellationToken = default)
    {
        await _context.Set<SalesOrder>().AddAsync(order, cancellationToken);
    }

    public Task<string> NextNumberAsync(string documentType, CancellationToken cancellationToken = default)
    {
        return NextDocumentNumberAsync(_context, documentType, cancellationToken);
    }

    public async Task AddCostingAsync(CostingRequest costing, CancellationToken cancellationToken = default)
    {
        await _context.Set<CostingRequest>().AddAsync(costing, cancellationToken);
    }

    public void RemoveLines(IEnumerable<SalesOrderLine> lines)
    {
        _context.Set<SalesOrderLine>().RemoveRange(lines);
    }

    public Task<CostingRequest?> FindCostingByOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        return _context.Set<CostingRequest>()
            .FirstOrDefaultAsync(costing => costing.SalesOrderId == salesOrderId, cancellationToken);
    }

    public void RemoveCosting(CostingRequest costing)
    {
        _context.Set<CostingRequest>().Remove(costing);
    }

    public void RemoveOrder(SalesOrder order)
    {
        _context.Set<SalesOrder>().Remove(order);
    }

    public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<InventoryItem?> FindInventoryItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<InventoryItem>()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await _context.Set<StockMovement>().AddAsync(movement, cancellationToken);
    }

    private static async Task<string> NextDocumentNumberAsync(
        SqlDbContext context,
        string documentType,
        CancellationToken cancellationToken)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var sequence = await context.Set<DocumentSequence>()
            .FirstOrDefaultAsync(
                entry => entry.DocumentType == documentType && entry.Year == year,
                cancellationToken);

        if (sequence is null)
        {
            sequence = DocumentSequence.Create(documentType, year);
            await context.Set<DocumentSequence>().AddAsync(sequence, cancellationToken);
        }

        var next = sequence.Next();
        return string.Format(DocumentSequenceTypes.NumberFormat, documentType, year, next);
    }
}
