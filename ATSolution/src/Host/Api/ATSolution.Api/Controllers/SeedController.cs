using ATSolution.Api.Seeding;
using ATSolution.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATSolution.Api.Controllers;

[Authorize]
[ApiController]
[Route(ApiRoutes.Admin.Seed)]
public sealed class SeedController : ControllerBase
{
    private readonly ICommercialDataSeeder _seeder;
    private readonly IConfiguration _configuration;

    public SeedController(ICommercialDataSeeder seeder, IConfiguration configuration)
    {
        _seeder = seeder;
        _configuration = configuration;
    }

    /// <summary>Returns whether mock data is present and current entity counts.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<SeedStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        return Ok(await _seeder.GetStatusAsync(cancellationToken));
    }

    /// <summary>
    /// Runs commercial mock seeding from each module's embedded SeedData JSON.
    /// Pass force=true to clear the seed marker check — still skips inserts that would violate unique keys;
    /// prefer a fresh database for a full re-seed.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SeedResultDto>> Run(
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("Seeding:SeedMockData", false)
            && !_configuration.GetValue("Seeding:AllowManualSeed", true))
        {
            return Conflict(new { message = "Seeding is disabled in configuration." });
        }

        var result = await _seeder.SeedAsync(force, cancellationToken);
        return Ok(result);
    }
}
