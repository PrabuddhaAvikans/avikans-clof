namespace Identity.Api.DTOs.Responses;

public sealed record UserResponseDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset ModifiedOnUtc);