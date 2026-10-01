namespace Customers.Application.Customers;

public sealed record ContactPersonDto(
    Guid Id,
    string Name,
    string? Title,
    string Email,
    string Phone,
    bool IsPrimary);
