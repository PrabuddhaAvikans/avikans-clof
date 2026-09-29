using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.SharedKernel.Constants;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPermissionResolver _permissionResolver;
    private readonly IApplicationValidator _validator;

    public AuthService(
        IIdentityRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IPermissionResolver permissionResolver,
        IApplicationValidator validator)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _permissionResolver = permissionResolver;
        _validator = validator;
    }

    public async Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = new LoginCommand(
            command.Email?.Trim() ?? string.Empty,
            command.Password ?? string.Empty);

        await _validator.ValidateAsync(normalized, cancellationToken);

        var user = await _users.Query()
            .Include(entity => entity.Role)
            .Include(entity => entity.UserRoleGroups)
            .FirstOrDefaultAsync(
                entity => entity.Email == normalized.Email,
                cancellationToken);

        if (user is null
            || user.Status != EntityStatuses.Active
            || !_passwordHasher.Verify(normalized.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(IdentityMessages.InvalidCredentials);
        }

        var permissions = await _permissionResolver.GetEffectivePermissionsAsync(user, cancellationToken);
        var authUser = new AuthUserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Role.Name,
            permissions);

        var token = _jwtTokenGenerator.GenerateToken(user, authUser);

        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(token, authUser);
    }
}
