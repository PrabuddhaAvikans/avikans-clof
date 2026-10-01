using ATSolution.SharedKernel.Constants;
using Identity.Application.Abstractions;
using Identity.Application.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Identity.Permissions)]
[ApiController]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionCatalogService _permissionCatalog;

    public PermissionsController(IPermissionCatalogService permissionCatalog)
    {
        _permissionCatalog = permissionCatalog;
    }

    [HttpGet]
    public ActionResult<PermissionCatalogDto> GetCatalog() =>
        Ok(_permissionCatalog.GetCatalog());

    [HttpGet(ApiRoutes.Identity.ByModule)]
    public ActionResult<IReadOnlyList<PermissionModuleGroupDto>> GetByModule() =>
        Ok(_permissionCatalog.GetByModule());
}
