using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Reprocessing;

public sealed record CancelReprocessingCommand(Guid Id, string? Reason = null);
