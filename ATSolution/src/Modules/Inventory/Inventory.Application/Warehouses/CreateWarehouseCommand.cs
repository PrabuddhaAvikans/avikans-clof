using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Warehouses;

public sealed record CreateWarehouseCommand(string Code, string Name, string? Address, string Status);
