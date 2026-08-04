using ATSolution.Application.Abstractions.Persistence;
using ATSolution.SharedKernel.Constants;
using AutoMapper;
using Identity.Application.Abstractions;
using Identity.Application.Users;
using Identity.Domain.Users;

namespace Identity.Application.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public IdentityService(
        IIdentityRepository identityRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _identityRepository = identityRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<string> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _identityRepository.ListAsync(cancellationToken);

        return users.Count == 0
            ? IdentityMessages.ModuleRunning
            : string.Format(IdentityMessages.ModuleRunningWithUsers, users.Count);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _identityRepository.ListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<UserDto>>(users);
    }

    public async Task<UserDto?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _identityRepository.GetByIdAsync(id, cancellationToken);

        return user is null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _identityRepository.GetByEmailAsync(email, cancellationToken);

        return user is null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _identityRepository.GetByEmailAsync(
            command.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email));
        }

        var user = _mapper.Map<User>(command);

        await _identityRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> UpdateUserAsync(
        string email,
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _identityRepository.GetByEmailForUpdateAsync(email, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _identityRepository.GetByEmailAsync(
                command.Email,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new InvalidOperationException(string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email));
            }
        }

        _mapper.Map(command, user);

        _identityRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> PatchUserAsync(
        string email,
        PatchUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _identityRepository.GetByEmailForUpdateAsync(email, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (command.Email is not null &&
            !string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _identityRepository.GetByEmailAsync(
                command.Email,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new InvalidOperationException(string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email));
            }
        }

        _mapper.Map(command, user);

        _identityRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteUserAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _identityRepository.GetByEmailForUpdateAsync(email, cancellationToken);

        if (user is null)
        {
            return false;
        }

        _identityRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}