using System.Text.Json;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<ProductVersion, Guid> _versions;
    private readonly IRepository<Category, Guid> _categories;
    private readonly IRepository<Brand, Guid> _brands;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public ProductService(
        IRepository<Product, Guid> products,
        IRepository<ProductVersion, Guid> versions,
        IRepository<Category, Guid> categories,
        IRepository<Brand, Guid> brands,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _products = products;
        _versions = versions;
        _categories = categories;
        _brands = brands;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<ProductDto>> ListAsync(
        ProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);

        var products = _products.Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Versions)
            .AsQueryable();

        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == query.CategoryId);
        }

        if (query.BrandId.HasValue)
        {
            products = products.Where(p => p.BrandId == query.BrandId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            products = products.Where(p => p.Status == query.Status);
        }

        if (query.AvailableForCustomerId.HasValue)
        {
            var customerId = query.AvailableForCustomerId.Value;
            products = products.Where(p => p.CustomerId == null || p.CustomerId == customerId);
        }
        else if (query.CustomerId.HasValue)
        {
            products = products.Where(p => p.CustomerId == query.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(p =>
                p.Name.Contains(search)
                || p.Sku.Contains(search)
                || (p.Description != null && p.Description.Contains(search))
                || p.Category.Name.Contains(search)
                || (p.Brand != null && p.Brand.Name.Contains(search)));
        }

        products = products.OrderByDescending(p => p.ModifiedOnUtc);

        var all = await products.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.VersionStatus))
        {
            all = all.Where(p =>
            {
                var current = ResolveCurrentVersion(p);
                return current is not null && current.Status == query.VersionStatus;
            }).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Tags))
        {
            var tags = query.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            all = all.Where(p =>
            {
                var current = ResolveCurrentVersion(p);
                if (current is null) return false;
                var versionTags = JsonColumn.Deserialize(current.TagsJson, Array.Empty<string>());
                return tags.Any(t => versionTags.Contains(t, StringComparer.OrdinalIgnoreCase));
            }).ToList();
        }

        var totalCount = all.Count;
        var pageItems = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapProduct)
            .ToList();

        return PaginatedResponse<ProductDto>.Create(pageItems, totalCount, page, pageSize);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(id, cancellationToken, asNoTracking: true);
        return product is null ? null : MapProduct(product);
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);
        if (command.BrandId.HasValue)
        {
            await EnsureBrandExistsAsync(command.BrandId.Value, cancellationToken);
        }

        var skuExists = await _products.Query()
            .AnyAsync(p => p.Sku == command.Sku, cancellationToken);
        if (skuExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Sku), "Product SKU already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var product = Product.Create(
            command.Sku,
            command.Name,
            command.Description ?? string.Empty,
            command.CategoryId,
            command.BrandId,
            command.ProductType,
            command.Status,
            command.CustomerId,
            null,
            command.ProjectId,
            command.ProjectName);

        var tags = command.Tags ?? Array.Empty<string>();
        var attributes = command.Attributes ?? Array.Empty<ProductAttributeDto>();
        var specs = MergeSpecifications(command.Specifications, command.WeightKg, command.Dimensions);
        var margin = command.CostPrice <= 0
            ? 0
            : Math.Round((command.BasePrice - command.CostPrice) / command.CostPrice * 100m, 2);

        var version = ProductVersion.Create(
            product.Id,
            1,
            ProductVersionStatuses.Draft,
            JsonColumn.Serialize(specs),
            SerializeOrDefault(command.Bom, "[]"),
            SerializeOrDefault(command.Operations, "[]"),
            JsonColumn.Serialize(attributes),
            SerializeOrDefault(command.Images, "[]"),
            SerializeOrDefault(command.CostBreakdown, JsonColumn.Serialize(EmptyCostBreakdown())),
            JsonColumn.Serialize(tags),
            command.BasePrice,
            command.CostPrice,
            margin,
            command.LeadTimeDays,
            command.MinOrderQuantity,
            revisionNotes: command.RevisionNotes);

        product.Versions.Add(version);
        product.SetCurrentVersion(version.Id);

        await _products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<ProductDto> UpdateAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var product = await LoadProductAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Product '{command.Id}' was not found.");

        var editable = product.Versions.FirstOrDefault(v => ProductVersionStatuses.IsEditable(v.Status) && !v.IsLocked)
            ?? throw new ApplicationValidationException(
            [
                new ValidationError("version", "This product cannot be edited yet. Create a revision to make changes.", "Validation"),
            ]);

        if (command.CategoryId.HasValue)
        {
            await EnsureCategoryExistsAsync(command.CategoryId.Value, cancellationToken);
        }

        if (command.BrandId.HasValue)
        {
            await EnsureBrandExistsAsync(command.BrandId.Value, cancellationToken);
        }

        if (command.Sku is not null)
        {
            var skuExists = await _products.Query()
                .AnyAsync(p => p.Sku == command.Sku && p.Id != command.Id, cancellationToken);
            if (skuExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Sku), "Product SKU already exists.", ValidationErrorCodes.Conflict),
                ]);
            }
        }

        product.UpdateHeader(
            command.Sku,
            command.Name,
            command.Description,
            command.CategoryId,
            command.BrandId,
            clearBrand: false,
            command.ProductType,
            command.Status,
            command.CustomerId,
            clearCustomer: false,
            null,
            command.ProjectId,
            clearProject: false,
            command.ProjectName);

        ApplyVersionUpdates(editable, new UpdateProductVersionCommand(
            product.Id,
            editable.Id,
            command.Specifications,
            command.Bom,
            command.Operations,
            command.Attributes,
            command.CostBreakdown,
            command.BasePrice,
            command.CostPrice,
            command.LeadTimeDays,
            command.MinOrderQuantity,
            command.Tags,
            command.RevisionNotes,
            command.Images));

        if (command.WeightKg.HasValue || command.Dimensions is not null || command.Specifications.HasValue)
        {
            var specs = JsonColumn.Deserialize(editable.SpecificationsJson, new ProductSpecificationsDto());
            specs = specs with
            {
                WeightKg = command.WeightKg ?? specs.WeightKg,
                Dimensions = command.Dimensions ?? specs.Dimensions,
            };
            if (command.Specifications.HasValue)
            {
                var incoming = JsonColumn.Deserialize(
                    command.Specifications.Value.GetRawText(),
                    new ProductSpecificationsDto());
                specs = MergeSpecDto(specs, incoming);
            }

            editable.UpdateContent(
                status: null,
                specificationsJson: JsonColumn.Serialize(specs),
                bomJson: null,
                operationsJson: null,
                attributesJson: null,
                imagesJson: null,
                costBreakdownJson: null,
                tagsJson: null,
                sellingPrice: null,
                costPrice: null,
                marginPercent: null,
                leadTimeDays: null,
                minOrderQuantity: null,
                notes: null,
                revisionNotes: null);
        }

        product.SetCurrentVersion(editable.Id);
        product.Touch();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.Query()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");

        product.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductVersionDto?> GetVersionAsync(
        Guid productId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await _versions.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ProductId == productId && v.Id == versionId, cancellationToken);
        return version is null ? null : MapVersion(version);
    }

    public async Task<ProductDto> UpdateVersionAsync(
        UpdateProductVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var product = await LoadProductAsync(command.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product '{command.ProductId}' was not found.");

        var version = product.Versions.FirstOrDefault(v => v.Id == command.VersionId)
            ?? throw new NotFoundException($"Product version '{command.VersionId}' was not found.");

        if (!ProductVersionStatuses.IsEditable(version.Status) || version.IsLocked)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("version", "This product version is locked. Create a revision to make changes.", "Validation"),
            ]);
        }

        ApplyVersionUpdates(version, command);
        product.SetCurrentVersion(version.Id);
        product.Touch();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<ProductDto> ReviseVersionAsync(
        ReviseProductVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(command.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product '{command.ProductId}' was not found.");

        var source = product.Versions.FirstOrDefault(v => v.Id == command.SourceVersionId)
            ?? throw new NotFoundException($"Product version '{command.SourceVersionId}' was not found.");

        var nextNumber = product.Versions.Max(v => v.VersionNumber) + 1;
        var revision = source.CloneAsRevision(nextNumber, command.RevisionNotes);
        product.Versions.Add(revision);
        product.SetCurrentVersion(revision.Id);
        product.Touch();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<ProductDto> UpdateHeaderAsync(
        UpdateProductHeaderCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Product '{command.Id}' was not found.");

        if (command.CategoryId.HasValue)
        {
            await EnsureCategoryExistsAsync(command.CategoryId.Value, cancellationToken);
        }

        if (command.BrandId.HasValue)
        {
            await EnsureBrandExistsAsync(command.BrandId.Value, cancellationToken);
        }

        product.UpdateHeader(
            command.Sku,
            command.Name,
            command.Description,
            command.CategoryId,
            command.BrandId,
            clearBrand: false,
            command.ProductType,
            command.Status,
            command.CustomerId,
            clearCustomer: false,
            null,
            command.ProjectId,
            clearProject: false,
            command.ProjectName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    private static void ApplyVersionUpdates(ProductVersion version, UpdateProductVersionCommand command)
    {
        var selling = command.BasePrice;
        var cost = command.CostPrice;
        decimal? margin = null;
        if (selling.HasValue || cost.HasValue)
        {
            var nextSell = selling ?? version.SellingPrice;
            var nextCost = cost ?? version.CostPrice;
            margin = nextCost <= 0 ? 0 : Math.Round((nextSell - nextCost) / nextCost * 100m, 2);
        }

        version.UpdateContent(
            command.Status,
            command.Specifications.HasValue ? command.Specifications.Value.GetRawText() : null,
            command.Bom.HasValue ? command.Bom.Value.GetRawText() : null,
            command.Operations.HasValue ? command.Operations.Value.GetRawText() : null,
            command.Attributes is null ? null : JsonColumn.Serialize(command.Attributes),
            command.Images.HasValue ? command.Images.Value.GetRawText() : null,
            command.CostBreakdown.HasValue ? command.CostBreakdown.Value.GetRawText() : null,
            command.Tags is null ? null : JsonColumn.Serialize(command.Tags),
            selling,
            cost,
            margin,
            command.LeadTimeDays,
            command.MinOrderQuantity,
            notes: null,
            command.RevisionNotes);
    }

    private async Task<Product?> LoadProductAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        IQueryable<Product> query = _products.Query()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Versions);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var exists = await _categories.Query().AnyAsync(c => c.Id == categoryId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Category '{categoryId}' was not found.");
        }
    }

    private async Task EnsureBrandExistsAsync(Guid brandId, CancellationToken cancellationToken)
    {
        var exists = await _brands.Query().AnyAsync(b => b.Id == brandId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Brand '{brandId}' was not found.");
        }
    }

    private static ProductVersion? ResolveCurrentVersion(Product product)
    {
        if (product.CurrentVersionId is Guid currentId)
        {
            var current = product.Versions.FirstOrDefault(v => v.Id == currentId);
            if (current is not null) return current;
        }

        return product.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
    }

    private static ProductDto MapProduct(Product product)
    {
        var versions = product.Versions
            .OrderBy(v => v.VersionNumber)
            .Select(MapVersion)
            .ToList();

        var current = versions.FirstOrDefault(v => v.Id == product.CurrentVersionId)
            ?? versions.LastOrDefault();

        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description ?? string.Empty,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.BrandId ?? Guid.Empty,
            product.Brand?.Name ?? string.Empty,
            product.ProductType,
            product.CustomerId,
            product.CustomerName,
            product.ProjectId,
            product.ProjectName,
            product.Currency,
            product.Status,
            current?.Id ?? Guid.Empty,
            versions,
            current?.BasePrice ?? 0,
            current?.CostPrice ?? 0,
            current?.Images ?? [],
            current?.Bom ?? [],
            current?.Operations ?? [],
            current?.Attributes ?? [],
            current?.Specifications.WeightKg,
            current?.Specifications.Dimensions,
            current?.LeadTimeDays ?? 0,
            current?.MinOrderQuantity ?? 1,
            current?.Tags ?? [],
            product.CreatedOnUtc,
            product.ModifiedOnUtc,
            product.CreatedBy);
    }

    private static ProductVersionDto MapVersion(ProductVersion version)
    {
        var specs = JsonColumn.Deserialize(version.SpecificationsJson, new ProductSpecificationsDto());
        var bom = JsonColumn.Deserialize(version.BomJson, Array.Empty<BomItemDto>());
        var operations = JsonColumn.Deserialize(version.OperationsJson, Array.Empty<ProductOperationDto>());
        var attributes = JsonColumn.Deserialize(version.AttributesJson, Array.Empty<ProductAttributeDto>());
        var images = JsonColumn.Deserialize(version.ImagesJson, Array.Empty<ProductImageDto>());
        var cost = JsonColumn.Deserialize(version.CostBreakdownJson, EmptyCostBreakdown());
        var tags = JsonColumn.Deserialize(version.TagsJson, Array.Empty<string>());

        return new ProductVersionDto(
            version.Id,
            version.ProductId,
            version.VersionNumber,
            version.Label,
            version.Status,
            version.IsLocked,
            specs,
            bom,
            operations,
            attributes,
            images,
            cost,
            version.SellingPrice,
            version.CostPrice,
            version.LeadTimeDays,
            version.MinOrderQuantity,
            tags,
            version.RevisionNotes,
            version.CreatedOnUtc,
            version.ModifiedOnUtc,
            version.ReleasedAtUtc,
            version.ReleasedBy,
            version.ApprovedAtUtc);
    }

    private static CostBreakdownDto EmptyCostBreakdown() =>
        new(0, 0, 0, 0, 0, 0, [], "default", null);

    private static ProductSpecificationsDto MergeSpecifications(
        JsonElement? specifications,
        decimal? weightKg,
        string? dimensions)
    {
        var specs = specifications.HasValue
            ? JsonColumn.Deserialize(specifications.Value.GetRawText(), new ProductSpecificationsDto())
            : new ProductSpecificationsDto();

        return specs with
        {
            WeightKg = weightKg ?? specs.WeightKg,
            Dimensions = dimensions ?? specs.Dimensions,
        };
    }

    private static ProductSpecificationsDto MergeSpecDto(
        ProductSpecificationsDto current,
        ProductSpecificationsDto incoming) =>
        current with
        {
            WeightKg = incoming.WeightKg ?? current.WeightKg,
            Dimensions = incoming.Dimensions ?? current.Dimensions,
            Size = incoming.Size ?? current.Size,
            Shape = incoming.Shape ?? current.Shape,
            Design = incoming.Design ?? current.Design,
            Finish = incoming.Finish ?? current.Finish,
            Colour = incoming.Colour ?? current.Colour,
            Glass = incoming.Glass ?? current.Glass,
            Wiring = incoming.Wiring ?? current.Wiring,
            MountingType = incoming.MountingType ?? current.MountingType,
            MountingBracket = incoming.MountingBracket ?? current.MountingBracket,
            Voltage = incoming.Voltage ?? current.Voltage,
            Wattage = incoming.Wattage ?? current.Wattage,
            LedType = incoming.LedType ?? current.LedType,
            ColorTemperature = incoming.ColorTemperature ?? current.ColorTemperature,
            Driver = incoming.Driver ?? current.Driver,
            LengthMm = incoming.LengthMm ?? current.LengthMm,
            WidthMm = incoming.WidthMm ?? current.WidthMm,
            HeightMm = incoming.HeightMm ?? current.HeightMm,
            DiameterMm = incoming.DiameterMm ?? current.DiameterMm,
            LumenOutput = incoming.LumenOutput ?? current.LumenOutput,
            Efficacy = incoming.Efficacy ?? current.Efficacy,
            Cri = incoming.Cri ?? current.Cri,
            BeamAngle = incoming.BeamAngle ?? current.BeamAngle,
            IpRating = incoming.IpRating ?? current.IpRating,
            InputVoltage = incoming.InputVoltage ?? current.InputVoltage,
            PowerFactor = incoming.PowerFactor ?? current.PowerFactor,
            Dimming = incoming.Dimming ?? current.Dimming,
            OpTempMin = incoming.OpTempMin ?? current.OpTempMin,
            OpTempMax = incoming.OpTempMax ?? current.OpTempMax,
            InputPower = incoming.InputPower ?? current.InputPower,
            InputCurrent = incoming.InputCurrent ?? current.InputCurrent,
            DriverType = incoming.DriverType ?? current.DriverType,
            DriverBrand = incoming.DriverBrand ?? current.DriverBrand,
            CoatingFinish = incoming.CoatingFinish ?? current.CoatingFinish,
            CoatingProcess = incoming.CoatingProcess ?? current.CoatingProcess,
            MaterialPrimary = incoming.MaterialPrimary ?? current.MaterialPrimary,
            MaterialSecondary = incoming.MaterialSecondary ?? current.MaterialSecondary,
            Certifications = incoming.Certifications ?? current.Certifications,
            Warranty = incoming.Warranty ?? current.Warranty,
            ManufacturingNotes = incoming.ManufacturingNotes ?? current.ManufacturingNotes,
            Accessories = incoming.Accessories ?? current.Accessories,
        };

    private static string SerializeOrDefault(JsonElement? element, string fallback) =>
        element.HasValue ? element.Value.GetRawText() : fallback;
}
