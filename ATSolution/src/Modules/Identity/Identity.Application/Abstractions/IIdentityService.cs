using Identity.Application.Users;

namespace Identity.Application.Abstractions;

public interface IIdentityService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateUserAsync(string email, UpdateUserCommand command, CancellationToken cancellationToken = default);

    Task<UserDto?> PatchUserAsync(string email, PatchUserCommand command, CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(string email, CancellationToken cancellationToken = default);

}