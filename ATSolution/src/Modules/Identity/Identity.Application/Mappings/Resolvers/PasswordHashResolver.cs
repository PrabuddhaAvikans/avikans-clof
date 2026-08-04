using AutoMapper;
using Identity.Application.Abstractions;
using Identity.Application.Users;
using Identity.Domain.Users;

namespace Identity.Application.Mappings.Resolvers;

internal sealed class PasswordHashResolver : IValueResolver<CreateUserCommand, User, string>
{
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHashResolver(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string Resolve(
        CreateUserCommand source,
        User destination,
        string destMember,
        ResolutionContext context) =>
        _passwordHasher.Hash(source.Password);
}