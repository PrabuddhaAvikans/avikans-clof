using ATSolution.SharedKernel.Constants;
using Configuration.Application.Abstractions;
using Configuration.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Configuration.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Configuration.SystemSettings)]
[ApiController]
public sealed class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _systemSettingsService;

    public SystemSettingsController(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _systemSettingsService.GetAsync(cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<SystemSettingsDto>> Update(
        [FromBody] UpdateSystemSettingsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _systemSettingsService.UpdateAsync(command, cancellationToken));
    }
}
