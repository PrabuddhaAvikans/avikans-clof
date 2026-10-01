using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Units;

public sealed record CreateUnitOfMeasureCommand(string Code, string Name, string Status);
