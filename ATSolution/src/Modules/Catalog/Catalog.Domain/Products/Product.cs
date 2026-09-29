using ATSolution.Domain.Entities.Common;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Common;

namespace Catalog.Domain.Products;

public class Product : Entity<Guid>, IAuditableEntity
{
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public Guid? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public string ProductType { get; private set; } = null!;
    public string Status { get; private set; } = EntityStatuses.Active;
    public Guid? CurrentVersionId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string? ProjectName { get; private set; }
    public string Currency { get; private set; } = "LKR";
    public string CreatedBy { get; private set; } = "system";
    public ICollection<ProductVersion> Versions { get; private set; } = new List<ProductVersion>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Product Create(
        string sku,
        string name,
        string? description,
        Guid categoryId,
        Guid? brandId,
        string productType,
        string status,
        Guid? customerId = null,
        string? customerName = null,
        Guid? projectId = null,
        string? projectName = null,
        string currency = "LKR",
        string createdBy = "system")
    {
        var now = DateTimeOffset.UtcNow;
        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim(),
            Name = name.Trim(),
            Description = description,
            CategoryId = categoryId,
            BrandId = brandId,
            ProductType = productType,
            Status = status,
            CustomerId = customerId,
            CustomerName = customerName,
            ProjectId = projectId,
            ProjectName = projectName,
            Currency = currency,
            CreatedBy = createdBy,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void SetCurrentVersion(Guid versionId)
    {
        CurrentVersionId = versionId;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateHeader(
        string? sku,
        string? name,
        string? description,
        Guid? categoryId,
        Guid? brandId,
        bool clearBrand,
        string? productType,
        string? status,
        Guid? customerId,
        bool clearCustomer,
        string? customerName,
        Guid? projectId,
        bool clearProject,
        string? projectName)
    {
        if (sku is not null) Sku = sku.Trim();
        if (name is not null) Name = name.Trim();
        if (description is not null) Description = description;
        if (categoryId.HasValue) CategoryId = categoryId.Value;
        if (clearBrand) BrandId = null;
        else if (brandId.HasValue) BrandId = brandId;
        if (productType is not null) ProductType = productType;
        if (status is not null) Status = status;
        if (clearCustomer)
        {
            CustomerId = null;
            CustomerName = null;
        }
        else
        {
            if (customerId.HasValue) CustomerId = customerId;
            if (customerName is not null) CustomerName = customerName;
        }

        if (clearProject)
        {
            ProjectId = null;
            ProjectName = null;
        }
        else
        {
            if (projectId.HasValue) ProjectId = projectId;
            if (projectName is not null) ProjectName = projectName;
        }

        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;
}
