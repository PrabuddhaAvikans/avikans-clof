using Identity.Application.Auth;

namespace Identity.Application.Abstractions;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
}
