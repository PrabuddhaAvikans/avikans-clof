namespace Inventory.Api.DTOs.Requests;

public sealed record CancelReprocessingRequestDto
{
    public string? Reason { get; init; }
}
