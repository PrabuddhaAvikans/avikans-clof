using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Warehouses;

public sealed record UpdateWarehouseCommand(Guid Id, string? Code = null, string? Name = null, string? Address = null, string? Status = null);
