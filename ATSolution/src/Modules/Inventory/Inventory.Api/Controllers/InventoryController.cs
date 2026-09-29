using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Api.DTOs.Requests;
using Inventory.Application.Abstractions;
using Inventory.Application.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Inventory.Items)]
[ApiController]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<InventoryItemDto>>> List(
        [FromQuery] InventoryListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.ListAsync(query, cancellationToken));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> GetLowStock(CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.GetLowStockAsync(cancellationToken));
    }

    [HttpGet("by-sku/{sku}")]
    public async Task<ActionResult<InventoryItemDto>> FindBySku(string sku, CancellationToken cancellationToken)
    {
        var item = await _inventoryService.FindBySkuAsync(sku, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItemDto>> Create(
        [FromBody] CreateInventoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var item = await _inventoryService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> Update(
        Guid id,
        [FromBody] UpdateInventoryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var item = await _inventoryService.UpdateAsync(
            new UpdateInventoryItemCommand(
                id,
                request.Sku,
                request.Name,
                request.Description,
                request.Category,
                request.ItemType,
                request.Unit,
                request.Brand,
                request.Supplier,
                request.TaxCode,
                request.QuantityOnHand,
                request.Warehouse,
                request.Location,
                request.MinStock,
                request.MaxStock,
                request.ReorderLevel,
                request.ReorderQuantity,
                request.BuyingPrice,
                request.CostPrice,
                request.PricingMethod,
                request.MarkupPercent,
                request.MarkupFixedAmount,
                request.SellingPrice,
                request.PricingEffectiveDate,
                request.Status),
            cancellationToken);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _inventoryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Inventory item deleted successfully."));
    }

    [HttpGet("{id:guid}/price-history")]
    public async Task<ActionResult<IReadOnlyList<InventoryPriceHistoryDto>>> GetPriceHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.GetPriceHistoryAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/movements")]
    public async Task<ActionResult<StockMovementDto>> RecordMovement(
        Guid id,
        [FromBody] RecordMovementRequestDto request,
        CancellationToken cancellationToken)
    {
        var movement = await _inventoryService.RecordMovementAsync(
            new RecordStockMovementCommand(
                id,
                request.Type,
                request.Quantity,
                request.ReferenceType,
                request.ReferenceId,
                request.Notes,
                request.Trace,
                request.PerformedBy,
                request.PerformedByName),
            cancellationToken);
        return Ok(movement);
    }
}

[Authorize]
[Route(ApiRoutes.Inventory.Movements)]
[ApiController]
public sealed class StockMovementsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public StockMovementsController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StockMovementDto>>> List(
        [FromQuery] StockMovementListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.ListMovementsAsync(query, cancellationToken));
    }
}
