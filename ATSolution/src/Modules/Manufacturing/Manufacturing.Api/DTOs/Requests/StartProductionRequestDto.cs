namespace Manufacturing.Api.DTOs.Requests;

public sealed record StartProductionRequestDto(IReadOnlyList<Guid>? Ids);
