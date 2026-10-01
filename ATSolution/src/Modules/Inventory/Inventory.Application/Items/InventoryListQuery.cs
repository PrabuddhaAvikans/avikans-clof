using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Items;

public sealed class InventoryListQuery : PaginatedRequest
{
    public string? Category { get; set; }
    public string? ItemType { get; set; }
    public string? StockStatus { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public string? Warehouse { get; set; }
}
