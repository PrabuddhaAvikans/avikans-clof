using Identity.Application.Users;

namespace Identity.Application.Abstractions;

public interface IIdentityService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateUserAsync(UpdateUserCommand command, CancellationToken cancellationToken = default);

    Task<UserDto> PatchUserAsync(PatchUserCommand command, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(DeleteUserCommand command, CancellationToken cancellationToken = default);
}
