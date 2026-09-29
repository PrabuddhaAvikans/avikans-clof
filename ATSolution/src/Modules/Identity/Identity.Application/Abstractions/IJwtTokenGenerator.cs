using Identity.Application.Auth;
using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, AuthUserDto authUser);
}
