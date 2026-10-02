using ATSolution.Application;
using ATSolution.Application.Abstractions.Periods;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Models;
using Delivery.Application.Abstractions;
using Delivery.Application.Common;
using Delivery.Application.Deliveries;
using Delivery.Domain.Common;
using Delivery.Domain.Sequences;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Common;
using Sales.Domain.SalesOrders;

namespace Delivery.Application.Services;

public sealed class DeliveryService : IDeliveryService
{
    private readonly IRepository<DeliveryEntity, Guid> _deliveries;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<User, Guid> _users;
    private readonly IRepository<DocumentSequence, Guid> _sequences;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;
    private readonly IBusinessPeriodGuard _periodGuard;

    public DeliveryService(
        IRepository<DeliveryEntity, Guid> deliveries,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<User, Guid> users,
        IRepository<DocumentSequence, Guid> sequences,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator,
        IBusinessPeriodGuard periodGuard)
    {
        _deliveries = deliveries;
        _salesOrders = salesOrders;
        _users = users;
        _sequences = sequences;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _periodGuard = periodGuard;
    }

    public async Task<PaginatedResponse<DeliveryDto>> ListAsync(
        DeliveryListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _deliveries.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(x => x.Status == query.Status);
        if (query.SalesOrderId.HasValue)
            items = items.Where(x => x.SalesOrderId == query.SalesOrderId);
        if (query.CustomerId.HasValue)
            items = items.Where(x => x.CustomerId == query.CustomerId);
        if (!string.IsNullOrWhiteSpace(query.Priority))
            items = items.Where(x => x.Priority == query.Priority);
        if (query.DriverId.HasValue)
            items = items.Where(x => x.DriverUserId == query.DriverId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.Number.Contains(search)
                || x.CustomerName.Contains(search)
                || x.SalesOrderNumber.Contains(search)
                || (x.TrackingNumber != null && x.TrackingNumber.Contains(search)));
        }

        items = items.OrderByDescending(x => x.ModifiedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<DeliveryDto>.Create(
            pageItems.Select(DeliveryMappers.MapDelivery).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<DeliveryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadDeliveryAsync(id, cancellationToken, asNoTracking: true);
        return entity is null ? null : DeliveryMappers.MapDelivery(entity);
    }

    public async Task<DeliveryDto> CreateAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        await _periodGuard.EnsureWritableAsync(command.ScheduledDate == default ? DateTimeOffset.UtcNow : command.ScheduledDate, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _salesOrders.Query()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
                ?? throw new NotFoundException($"Sales order '{command.SalesOrderId}' was not found.");

            if (!SalesOrderStatuses.CanDeliver(order.Status))
            {
                DeliveryErrors.InvalidState(
                    "Confirm the sales order before creating a delivery.");
            }

            ValidateDeliveryQuantities(order, command.Items);

            var (driverUserId, driverName) = await ResolveDriverAsync(command.DriverId, ct);
            var lineItemsJson = DeliveryMappers.SerializeItems(command.Items);
            var shippingJson = order.ShippingAddressJson ?? order.BillingAddressJson;
            var number = await DocumentNumberGenerator.NextDeliveryNumberAsync(_sequences, ct);

            var delivery = DeliveryEntity.Create(
                number,
                order.Id,
                order.Number,
                order.CustomerId,
                order.CustomerName,
                command.Priority,
                command.ScheduledDate,
                lineItemsJson,
                shippingJson,
                driverUserId,
                driverName,
                command.VehicleNumber,
                command.Carrier,
                command.Notes,
                command.CreatedBy ?? "system",
                command.CreatedByName ?? "System");

            await _deliveries.AddAsync(delivery, ct);
            order.LinkDelivery(delivery.Id);

            await _unitOfWork.SaveChangesAsync(ct);
            return DeliveryMappers.MapDelivery(delivery);
        }, cancellationToken);
    }

    public async Task<DeliveryDto> UpdateAsync(
        UpdateDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await LoadDeliveryAsync(command.Id, ct)
                ?? throw new NotFoundException($"Delivery '{command.Id}' was not found.");

            string? lineItemsJson = null;
            if (command.Items is not null)
            {
                var order = await _salesOrders.Query()
                    .Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == entity.SalesOrderId, ct)
                    ?? throw new NotFoundException($"Sales order '{entity.SalesOrderId}' was not found.");

                ValidateDeliveryQuantities(order, command.Items);
                lineItemsJson = DeliveryMappers.SerializeItems(command.Items);
            }

            Guid? driverUserId = command.DriverId;
            string? driverName = null;
            if (command.DriverId.HasValue)
            {
                (_, driverName) = await ResolveDriverAsync(command.DriverId, ct);
            }

            entity.Update(
                command.Priority,
                command.ScheduledDate,
                lineItemsJson,
                driverUserId,
                driverName,
                command.VehicleNumber,
                command.Carrier,
                command.Notes);

            await _unitOfWork.SaveChangesAsync(ct);
            return DeliveryMappers.MapDelivery(entity);
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await LoadDeliveryAsync(id, ct)
                ?? throw new NotFoundException($"Delivery '{id}' was not found.");

            if (entity.Status != DeliveryStatuses.Planned)
            {
                DeliveryErrors.InvalidState("Only planned deliveries can be deleted.");
            }

            _deliveries.Remove(entity);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<DeliveryDto> UpdateStatusAsync(
        UpdateDeliveryStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await LoadDeliveryAsync(command.Id, ct)
                ?? throw new NotFoundException($"Delivery '{command.Id}' was not found.");

            try
            {
                entity.SetStatus(command.Status);
            }
            catch (InvalidOperationException ex)
            {
                DeliveryErrors.InvalidState(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return DeliveryMappers.MapDelivery(entity);
        }, cancellationToken);
    }

    public async Task<DeliveryDto> DispatchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await LoadDeliveryAsync(id, ct)
                ?? throw new NotFoundException($"Delivery '{id}' was not found.");

            try
            {
                entity.Dispatch();
            }
            catch (InvalidOperationException ex)
            {
                DeliveryErrors.InvalidState(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return DeliveryMappers.MapDelivery(entity);
        }, cancellationToken);
    }

    public async Task<DeliveryDto> RecordProofOfDeliveryAsync(
        RecordProofOfDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await LoadDeliveryAsync(command.Id, ct)
                ?? throw new NotFoundException($"Delivery '{command.Id}' was not found.");

            var proof = new ProofOfDeliveryDto(
                Guid.NewGuid(),
                command.SignedBy,
                command.SignedAt,
                command.SignatureUrl,
                command.PhotoUrls ?? [],
                command.Notes,
                command.GpsCoordinates);

            entity.RecordProofOfDelivery(JsonColumn.Serialize(proof));

            var order = await _salesOrders.Query()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == entity.SalesOrderId, ct)
                ?? throw new NotFoundException($"Sales order '{entity.SalesOrderId}' was not found.");

            var items = DeliveryMappers.MapItems(entity.LineItemsJson);
            foreach (var item in items)
            {
                if (item.QuantityDelivered <= 0)
                {
                    continue;
                }

                var line = order.Lines.FirstOrDefault(l => l.ProductId == item.ProductId);
                if (line is null)
                {
                    continue;
                }

                var remaining = line.Quantity - line.QuantityDelivered;
                if (item.QuantityDelivered > remaining)
                {
                    DeliveryErrors.InvalidState(
                        $"Delivery quantity for '{item.ProductName}' exceeds remaining sales order quantity ({remaining}).");
                }

                line.RecordDelivery(item.QuantityDelivered);
            }

            order.RefreshDeliveryStatus();

            await _unitOfWork.SaveChangesAsync(ct);
            return DeliveryMappers.MapDelivery(entity);
        }, cancellationToken);
    }

    private static void ValidateDeliveryQuantities(
        SalesOrder order,
        IReadOnlyList<DeliveryItemInputDto> items)
    {
        foreach (var item in items)
        {
            if (item.QuantityDelivered < 0)
            {
                DeliveryErrors.InvalidState(
                    $"Delivery quantity for '{item.ProductName}' cannot be negative.");
            }

            if (item.QuantityDelivered <= 0)
            {
                continue;
            }

            var line = order.Lines.FirstOrDefault(l => l.ProductId == item.ProductId)
                ?? throw new ApplicationValidationException(
                [
                    new ValidationError(
                        "items",
                        $"Product '{item.ProductName}' is not on sales order '{order.Number}'.",
                        "INVALID_STATE"),
                ]);

            var remaining = line.Quantity - line.QuantityDelivered;
            if (item.QuantityDelivered > remaining)
            {
                DeliveryErrors.InvalidState(
                    $"Delivery quantity for '{item.ProductName}' exceeds remaining sales order quantity ({remaining}).");
            }

            if (item.QuantityDelivered > item.QuantityOrdered && item.QuantityOrdered > 0)
            {
                DeliveryErrors.InvalidState(
                    $"Delivery quantity for '{item.ProductName}' exceeds ordered quantity ({item.QuantityOrdered}).");
            }
        }
    }

    private async Task<(Guid? UserId, string? Name)> ResolveDriverAsync(
        Guid? driverId,
        CancellationToken cancellationToken)
    {
        if (!driverId.HasValue)
        {
            return (null, null);
        }

        var user = await _users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == driverId.Value, cancellationToken)
            ?? throw new NotFoundException($"User '{driverId}' was not found.");

        return (user.Id, user.FullName);
    }

    private async Task<DeliveryEntity?> LoadDeliveryAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        var query = _deliveries.Query().AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
