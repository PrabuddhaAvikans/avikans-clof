using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Units;

public sealed record UpdateUnitOfMeasureCommand(Guid Id, string? Code = null, string? Name = null, string? Status = null);
