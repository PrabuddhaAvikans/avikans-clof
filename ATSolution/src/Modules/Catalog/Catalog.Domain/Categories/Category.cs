using ATSolution.Domain.Entities.Common;
using Catalog.Domain.Common;

namespace Catalog.Domain.Categories;

public class Category : Entity<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid? ParentId { get; private set; }
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public int SortOrder { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public string? ImageUrl { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Category Create(
        string name,
        string slug,
        string? description,
        Guid? parentId,
        int sortOrder,
        string status,
        string? imageUrl = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim(),
            Description = description,
            ParentId = parentId,
            SortOrder = sortOrder,
            Status = status,
            ImageUrl = imageUrl,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string name,
        string slug,
        string? description,
        Guid? parentId,
        int sortOrder,
        string status,
        string? imageUrl)
    {
        Name = name.Trim();
        Slug = slug.Trim();
        Description = description;
        ParentId = parentId;
        SortOrder = sortOrder;
        Status = status;
        ImageUrl = imageUrl;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ApplyPartial(
        string? name,
        string? slug,
        string? description,
        Guid? parentId,
        bool clearParent,
        int? sortOrder,
        string? status,
        string? imageUrl)
    {
        if (name is not null) Name = name.Trim();
        if (slug is not null) Slug = slug.Trim();
        if (description is not null) Description = description;
        if (clearParent) ParentId = null;
        else if (parentId.HasValue) ParentId = parentId;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        if (status is not null) Status = status;
        if (imageUrl is not null) ImageUrl = imageUrl;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
