namespace Identity.Application.Users;

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset ModifiedOnUtc);