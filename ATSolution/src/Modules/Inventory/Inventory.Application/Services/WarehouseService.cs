using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Application.Abstractions;
using Inventory.Application.Warehouses;
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services;

public sealed class WarehouseService : IWarehouseService
{
    private readonly IRepository<Warehouse, Guid> _warehouses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public WarehouseService(
        IRepository<Warehouse, Guid> warehouses,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<WarehouseDto>> ListAsync(
        WarehouseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var warehouses = _warehouses.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            warehouses = warehouses.Where(w => w.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            warehouses = warehouses.Where(w =>
                w.Name.Contains(search)
                || w.Code.Contains(search)
                || (w.Address != null && w.Address.Contains(search)));
        }

        warehouses = warehouses.OrderBy(w => w.Name);
        var totalCount = await warehouses.CountAsync(cancellationToken);
        var items = await warehouses.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<WarehouseDto>.Create(items.Select(Map).ToList(), totalCount, page, pageSize);
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouses.Query().AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        return warehouse is null ? null : Map(warehouse);
    }

    public async Task<WarehouseDto> CreateAsync(
        CreateWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var codeExists = await _warehouses.Query().AnyAsync(w => w.Code == command.Code, cancellationToken);
        if (codeExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Code), "Warehouse code already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var warehouse = Warehouse.Create(command.Code, command.Name, command.Address, command.Status);
        await _warehouses.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(
        UpdateWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var warehouse = await _warehouses.Query()
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Warehouse '{command.Id}' was not found.");

        if (command.Code is not null)
        {
            var codeExists = await _warehouses.Query()
                .AnyAsync(w => w.Code == command.Code && w.Id != command.Id, cancellationToken);
            if (codeExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Code), "Warehouse code already exists.", ValidationErrorCodes.Conflict),
                ]);
            }
        }

        warehouse.Update(
            command.Code ?? warehouse.Code,
            command.Name ?? warehouse.Name,
            command.Address ?? warehouse.Address,
            command.Status ?? warehouse.Status);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(warehouse);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouses.Query()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Warehouse '{id}' was not found.");

        warehouse.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static WarehouseDto Map(Warehouse warehouse) =>
        new(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.Status, warehouse.CreatedOnUtc, warehouse.ModifiedOnUtc);
}
