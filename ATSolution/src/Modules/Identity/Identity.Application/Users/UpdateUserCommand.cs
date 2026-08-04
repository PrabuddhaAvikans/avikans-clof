namespace Identity.Application.Users;

public sealed record UpdateUserCommand(
    string FirstName,
    string LastName,
    string Email);