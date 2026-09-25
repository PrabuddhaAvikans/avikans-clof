namespace Identity.Application.Users;

public sealed record PatchUserCommand(
    string? FirstName,
    string? LastName,
    string? Email,
    string CurrentEmail = "");
