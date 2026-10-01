using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Items;

public sealed class StockMovementListQuery : PaginatedRequest
{
    public Guid? InventoryItemId { get; set; }
    public string? Type { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
}
