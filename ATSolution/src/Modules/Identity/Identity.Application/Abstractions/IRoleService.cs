using ATSolution.SharedKernel.Models;
using Identity.Application.Roles;

namespace Identity.Application.Abstractions;

public interface IRoleService
{
    Task<PaginatedResponse<RoleDto>> ListAsync(
        RoleListQuery query,
        CancellationToken cancellationToken = default);

    Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(CreateRoleCommand command, CancellationToken cancellationToken = default);

    Task<RoleDto> UpdateAsync(UpdateRoleCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
