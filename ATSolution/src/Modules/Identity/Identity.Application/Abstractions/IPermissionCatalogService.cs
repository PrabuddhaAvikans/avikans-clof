using Identity.Application.Permissions;

namespace Identity.Application.Abstractions;

public interface IPermissionCatalogService
{
    PermissionCatalogDto GetCatalog();
    IReadOnlyList<PermissionModuleGroupDto> GetByModule();
}
