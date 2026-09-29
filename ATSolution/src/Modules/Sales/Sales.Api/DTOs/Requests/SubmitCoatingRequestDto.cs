using Sales.Application.Costing;

namespace Sales.Api.DTOs.Requests;

public sealed record SubmitCoatingRequestDto(
    IReadOnlyList<CoatingSubmitItemDto> Items,
    IReadOnlyList<EstimationMaterialInputDto>? Materials = null,
    string? Notes = null,
    string? ActorName = null);
