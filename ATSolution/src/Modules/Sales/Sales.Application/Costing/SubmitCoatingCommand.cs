using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Costing;

public sealed record SubmitCoatingCommand(
    Guid Id,
    IReadOnlyList<CoatingSubmitItemDto> Items,
    IReadOnlyList<EstimationMaterialInputDto>? Materials,
    string? Notes,
    string? ActorName);
