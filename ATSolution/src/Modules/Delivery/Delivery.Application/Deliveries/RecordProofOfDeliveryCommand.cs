using ATSolution.SharedKernel.Models;

namespace Delivery.Application.Deliveries;

public sealed record RecordProofOfDeliveryCommand(
    Guid Id,
    string SignedBy,
    DateTimeOffset SignedAt,
    string? SignatureUrl,
    IReadOnlyList<string>? PhotoUrls,
    string? Notes,
    GpsCoordinatesDto? GpsCoordinates);
