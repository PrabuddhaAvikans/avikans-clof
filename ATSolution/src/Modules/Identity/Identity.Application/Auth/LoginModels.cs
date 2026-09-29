namespace Identity.Application.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string Role,
    IReadOnlyList<string> Permissions);

public sealed record LoginResult(string Token, AuthUserDto User);

public sealed record LoginCommand(string Email, string Password);
