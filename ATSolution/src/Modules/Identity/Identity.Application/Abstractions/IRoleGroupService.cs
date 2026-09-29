using ATSolution.SharedKernel.Models;
using Identity.Application.RoleGroups;
using Identity.Application.Roles;

namespace Identity.Application.Abstractions;

public interface IRoleGroupService
{
    Task<PaginatedResponse<RoleGroupDto>> ListAsync(
        RoleListQuery query,
        CancellationToken cancellationToken = default);

    Task<RoleGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleGroupDto> CreateAsync(
        CreateRoleGroupCommand command,
        CancellationToken cancellationToken = default);

    Task<RoleGroupDto> UpdateAsync(
        UpdateRoleGroupCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
