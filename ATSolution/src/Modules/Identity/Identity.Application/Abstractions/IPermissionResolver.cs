using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IPermissionResolver
{
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        User user,
        CancellationToken cancellationToken = default);
}
