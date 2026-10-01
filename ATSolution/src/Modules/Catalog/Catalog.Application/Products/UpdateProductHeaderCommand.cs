using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Products;

public sealed record UpdateProductHeaderCommand(
    Guid Id,
    string? Sku = null,
    string? Name = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string? ProductType = null,
    Guid? CustomerId = null,
    Guid? ProjectId = null,
    string? ProjectName = null,
    string? Status = null);
