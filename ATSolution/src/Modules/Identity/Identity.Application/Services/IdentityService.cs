using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
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
    private readonly IApplicationValidator _validator;
    private readonly IMapper _mapper;

    public IdentityService(
        IIdentityRepository identityRepository,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator,
        IMapper mapper)
    {
        _identityRepository = identityRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
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
        var user = await _identityRepository.FirstOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

        return user is null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var user = _mapper.Map<User>(command);

        await _identityRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateUserAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var user = await _identityRepository.FirstOrDefaultAsync(
            entity => entity.Email == command.CurrentEmail,
            cancellationToken,
            asNoTracking: false);

        if (user is null)
        {
            throw new NotFoundException(
                string.Format(IdentityMessages.UserNotFoundByEmail, command.CurrentEmail));
        }

        _mapper.Map(command, user);
        _identityRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> PatchUserAsync(
        PatchUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var user = await _identityRepository.FirstOrDefaultAsync(
            entity => entity.Email == command.CurrentEmail,
            cancellationToken,
            asNoTracking: false);

        if (user is null)
        {
            throw new NotFoundException(
                string.Format(IdentityMessages.UserNotFoundByEmail, command.CurrentEmail));
        }

        _mapper.Map(command, user);
        _identityRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task DeleteUserAsync(
        DeleteUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var user = await _identityRepository.FirstOrDefaultAsync(
            entity => entity.Email == command.Email,
            cancellationToken,
            asNoTracking: false);

        if (user is null)
        {
            throw new NotFoundException(
                string.Format(IdentityMessages.UserNotFoundByEmail, command.Email));
        }

        _identityRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
