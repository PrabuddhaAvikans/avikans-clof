namespace Identity.Application.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string Role,
    IReadOnlyList<string> Permissions);
