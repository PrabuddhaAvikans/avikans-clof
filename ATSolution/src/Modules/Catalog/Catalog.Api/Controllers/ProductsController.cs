using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Api.DTOs.Requests;
using Catalog.Application.Abstractions;
using Catalog.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Catalog.Products)]
[ApiController]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProductDto>>> List(
        [FromQuery] ProductListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _productService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut(ApiRoutes.ById)]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromBody] UpdateProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateAsync(
            new UpdateProductCommand(
                id,
                request.Sku,
                request.Name,
                request.Description,
                request.CategoryId,
                request.BrandId,
                request.ProductType,
                request.Status,
                request.BasePrice,
                request.CostPrice,
                request.LeadTimeDays,
                request.MinOrderQuantity,
                request.Tags,
                request.Attributes,
                request.CustomerId,
                request.ProjectId,
                request.ProjectName,
                request.WeightKg,
                request.Dimensions,
                request.Specifications,
                request.Bom,
                request.Operations,
                request.CostBreakdown,
                request.RevisionNotes,
                request.Images),
            cancellationToken);
        return Ok(product);
    }

    [HttpDelete(ApiRoutes.ById)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Product deleted successfully."));
    }

    [HttpGet(ApiRoutes.Catalog.ProductVersion)]
    public async Task<ActionResult<ProductVersionDto>> GetVersion(
        Guid productId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await _productService.GetVersionAsync(productId, versionId, cancellationToken);
        return version is null ? NotFound() : Ok(version);
    }

    [HttpPut(ApiRoutes.Catalog.ProductVersion)]
    public async Task<ActionResult<ProductDto>> UpdateVersion(
        Guid productId,
        Guid versionId,
        [FromBody] UpdateProductVersionRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateVersionAsync(
            new UpdateProductVersionCommand(
                productId,
                versionId,
                request.Specifications,
                request.Bom,
                request.Operations,
                request.Attributes,
                request.CostBreakdown,
                request.BasePrice,
                request.CostPrice,
                request.LeadTimeDays,
                request.MinOrderQuantity,
                request.Tags,
                request.RevisionNotes,
                request.Images,
                request.Status),
            cancellationToken);
        return Ok(product);
    }

    [HttpPost(ApiRoutes.Catalog.ReviseProductVersion)]
    public async Task<ActionResult<ProductDto>> ReviseVersion(
        Guid productId,
        Guid sourceVersionId,
        [FromBody] ReviseVersionRequestDto? request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.ReviseVersionAsync(
            new ReviseProductVersionCommand(productId, sourceVersionId, request?.RevisionNotes),
            cancellationToken);
        return Ok(product);
    }

    [HttpPut(ApiRoutes.Catalog.ProductHeader)]
    public async Task<ActionResult<ProductDto>> UpdateHeader(
        Guid productId,
        [FromBody] UpdateProductHeaderCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateHeaderAsync(
            request with { Id = productId },
            cancellationToken);
        return Ok(product);
    }
}
