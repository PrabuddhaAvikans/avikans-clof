using Delivery.Application.Deliveries;

namespace Delivery.Api.DTOs.Requests;

public sealed record RecordProofOfDeliveryRequestDto(
    string SignedBy,
    DateTimeOffset SignedAt,
    string? SignatureUrl,
    IReadOnlyList<string>? PhotoUrls,
    string? Notes,
    GpsCoordinatesDto? GpsCoordinates);
