namespace Identity.Application.Users;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password);