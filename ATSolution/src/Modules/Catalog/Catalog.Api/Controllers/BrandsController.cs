using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Api.DTOs.Requests;
using Catalog.Application.Abstractions;
using Catalog.Application.Brands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Catalog.Brands)]
[ApiController]
public sealed class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BrandDto>>> List(
        [FromQuery] BrandListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _brandService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _brandService.GetByIdAsync(id, cancellationToken);
        return brand is null ? NotFound() : Ok(brand);
    }

    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create(
        [FromBody] CreateBrandCommand command,
        CancellationToken cancellationToken)
    {
        var brand = await _brandService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = brand.Id }, brand);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Update(
        Guid id,
        [FromBody] UpdateBrandRequestDto request,
        CancellationToken cancellationToken)
    {
        var brand = await _brandService.UpdateAsync(
            new UpdateBrandCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.Status,
                request.LogoUrl,
                request.Website,
                request.CountryOfOrigin),
            cancellationToken);
        return Ok(brand);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _brandService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Brand deleted successfully."));
    }
}
