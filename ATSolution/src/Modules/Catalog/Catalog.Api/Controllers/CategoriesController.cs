using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Api.DTOs.Requests;
using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Catalog.Categories)]
[ApiController]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryDto>>> List(
        [FromQuery] CategoryListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.ListAsync(query, cancellationToken));
    }

    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetTree(CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.GetTreeAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateAsync(
            new UpdateCategoryCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.ParentId,
                request.ClearParent,
                request.SortOrder,
                request.Status,
                request.ImageUrl),
            cancellationToken);
        return Ok(category);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Category deleted successfully."));
    }
}
