using ATSolution.Application.Abstractions.Audit;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.SharedKernel.Constants;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPermissionResolver _permissionResolver;
    private readonly IApplicationValidator _validator;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IIdentityRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IPermissionResolver permissionResolver,
        IApplicationValidator validator,
        IAuditEventWriter auditEventWriter,
        ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _permissionResolver = permissionResolver;
        _validator = validator;
        _auditEventWriter = auditEventWriter;
        _logger = logger;
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
            await TryWriteAuditAsync(
                new AuditEventWriteRequest(
                    UserId: "system",
                    UserName: "System",
                    Action: "failed_login",
                    Entity: "Session",
                    EntityId: string.IsNullOrWhiteSpace(normalized.Email) ? "unknown" : normalized.Email,
                    Details: string.IsNullOrWhiteSpace(normalized.Email)
                        ? "Failed login attempt"
                        : $"Failed login for {normalized.Email}",
                    Severity: "critical"),
                cancellationToken);

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

        await TryWriteAuditAsync(
            new AuditEventWriteRequest(
                UserId: user.Id.ToString(),
                UserName: user.FullName,
                Action: "login",
                Entity: "Session",
                EntityId: user.Id.ToString(),
                Details: "Signed in successfully",
                Severity: "info",
                EntityLabel: user.FullName),
            cancellationToken);

        return new LoginResult(token, authUser);
    }

    private async Task TryWriteAuditAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditEventWriter.WriteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit event {Action} for {EntityId}.", request.Action, request.EntityId);
        }
    }
}
