using System.Text.Json;

namespace Inventory.Api.DTOs.Requests;

public sealed record RecordMovementRequestDto
{
    public string Type { get; init; } = null!;
    public decimal Quantity { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public string? Notes { get; init; }
    public JsonElement? Trace { get; init; }
    public string? PerformedBy { get; init; }
    public string? PerformedByName { get; init; }
}
