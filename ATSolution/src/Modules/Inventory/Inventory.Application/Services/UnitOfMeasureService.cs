using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Application.Abstractions;
using Inventory.Application.Units;
using Inventory.Domain.Units;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services;

public sealed class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly IRepository<UnitOfMeasure, Guid> _units;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public UnitOfMeasureService(
        IRepository<UnitOfMeasure, Guid> units,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _units = units;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<UnitOfMeasureDto>> ListAsync(
        UnitOfMeasureListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var units = _units.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            units = units.Where(u => u.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            units = units.Where(u => u.Name.Contains(search) || u.Code.Contains(search));
        }

        units = units.OrderBy(u => u.Code);
        var totalCount = await units.CountAsync(cancellationToken);
        var items = await units.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<UnitOfMeasureDto>.Create(items.Select(Map).ToList(), totalCount, page, pageSize);
    }

    public async Task<UnitOfMeasureDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _units.Query().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return unit is null ? null : Map(unit);
    }

    public async Task<UnitOfMeasureDto> CreateAsync(
        CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var codeExists = await _units.Query().AnyAsync(u => u.Code == command.Code, cancellationToken);
        if (codeExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Code), "Unit of measure code already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var unit = UnitOfMeasure.Create(command.Code, command.Name, command.Status);
        await _units.AddAsync(unit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(unit);
    }

    public async Task<UnitOfMeasureDto> UpdateAsync(
        UpdateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var unit = await _units.Query()
            .FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Unit of measure '{command.Id}' was not found.");

        if (command.Code is not null)
        {
            var codeExists = await _units.Query()
                .AnyAsync(u => u.Code == command.Code && u.Id != command.Id, cancellationToken);
            if (codeExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Code), "Unit of measure code already exists.", ValidationErrorCodes.Conflict),
                ]);
            }
        }

        unit.Update(command.Code ?? unit.Code, command.Name ?? unit.Name, command.Status ?? unit.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(unit);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _units.Query()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Unit of measure '{id}' was not found.");

        unit.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UnitOfMeasureDto Map(UnitOfMeasure unit) =>
        new(unit.Id, unit.Code, unit.Name, unit.Status, unit.CreatedOnUtc, unit.ModifiedOnUtc);
}
