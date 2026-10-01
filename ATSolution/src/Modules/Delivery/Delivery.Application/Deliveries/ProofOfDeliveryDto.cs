namespace Delivery.Application.Deliveries;

public sealed record ProofOfDeliveryDto(
    Guid Id,
    string SignedBy,
    DateTimeOffset SignedAt,
    string? SignatureUrl,
    IReadOnlyList<string> PhotoUrls,
    string? Notes,
    GpsCoordinatesDto? GpsCoordinates);
