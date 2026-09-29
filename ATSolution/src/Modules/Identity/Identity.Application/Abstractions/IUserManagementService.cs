using ATSolution.SharedKernel.Models;
using Identity.Application.Users;

namespace Identity.Application.Abstractions;

public interface IUserManagementService
{
    Task<PaginatedResponse<UserDetailDto>> ListAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDetailDto> CreateAsync(
        CreateManagedUserCommand command,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto> UpdateAsync(
        UpdateManagedUserCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PermissionAssignmentDto> GetPermissionAssignmentAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
