using ATSolution.Domain.Entities.Common;
using Catalog.Domain.Common;

namespace Catalog.Domain.Products;

public class ProductVersion : Entity<Guid>, IAuditableEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int VersionNumber { get; private set; }
    public string Label { get; private set; } = null!;
    public string Status { get; private set; } = ProductVersionStatuses.Draft;
    public bool IsLocked { get; private set; }
    public string SpecificationsJson { get; private set; } = "{}";
    public string BomJson { get; private set; } = "[]";
    public string OperationsJson { get; private set; } = "[]";
    public string AttributesJson { get; private set; } = "[]";
    public string ImagesJson { get; private set; } = "[]";
    public string CostBreakdownJson { get; private set; } = "{}";
    public string TagsJson { get; private set; } = "[]";
    public decimal SellingPrice { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal MarginPercent { get; private set; }
    public int LeadTimeDays { get; private set; }
    public int MinOrderQuantity { get; private set; } = 1;
    public string? Notes { get; private set; }
    public string? RevisionNotes { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public string? ReleasedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static ProductVersion Create(
        Guid productId,
        int versionNumber,
        string status,
        string specificationsJson,
        string bomJson,
        string operationsJson,
        string attributesJson,
        string imagesJson,
        string costBreakdownJson,
        string tagsJson,
        decimal sellingPrice,
        decimal costPrice,
        decimal marginPercent,
        int leadTimeDays,
        int minOrderQuantity,
        string? notes = null,
        string? revisionNotes = null)
    {
        var now = DateTimeOffset.UtcNow;
        var locked = ProductVersionStatuses.IsImmutable(status);
        return new ProductVersion
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VersionNumber = versionNumber,
            Label = $"v{versionNumber}",
            Status = status,
            IsLocked = locked,
            SpecificationsJson = specificationsJson,
            BomJson = bomJson,
            OperationsJson = operationsJson,
            AttributesJson = attributesJson,
            ImagesJson = imagesJson,
            CostBreakdownJson = costBreakdownJson,
            TagsJson = tagsJson,
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            MarginPercent = marginPercent,
            LeadTimeDays = leadTimeDays,
            MinOrderQuantity = Math.Max(1, minOrderQuantity),
            Notes = notes,
            RevisionNotes = revisionNotes,
            ReleasedAtUtc = locked ? now : null,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateContent(
        string? status,
        string? specificationsJson,
        string? bomJson,
        string? operationsJson,
        string? attributesJson,
        string? imagesJson,
        string? costBreakdownJson,
        string? tagsJson,
        decimal? sellingPrice,
        decimal? costPrice,
        decimal? marginPercent,
        int? leadTimeDays,
        int? minOrderQuantity,
        string? notes,
        string? revisionNotes)
    {
        if (ProductVersionStatuses.IsImmutable(Status) || IsLocked)
        {
            throw new InvalidOperationException("Released product versions are immutable.");
        }

        if (status is not null)
        {
            Status = status;
            IsLocked = ProductVersionStatuses.IsImmutable(status);
            if (IsLocked)
            {
                ReleasedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        if (specificationsJson is not null) SpecificationsJson = specificationsJson;
        if (bomJson is not null) BomJson = bomJson;
        if (operationsJson is not null) OperationsJson = operationsJson;
        if (attributesJson is not null) AttributesJson = attributesJson;
        if (imagesJson is not null) ImagesJson = imagesJson;
        if (costBreakdownJson is not null) CostBreakdownJson = costBreakdownJson;
        if (tagsJson is not null) TagsJson = tagsJson;
        if (sellingPrice.HasValue) SellingPrice = sellingPrice.Value;
        if (costPrice.HasValue) CostPrice = costPrice.Value;
        if (marginPercent.HasValue) MarginPercent = marginPercent.Value;
        if (leadTimeDays.HasValue) LeadTimeDays = leadTimeDays.Value;
        if (minOrderQuantity.HasValue) MinOrderQuantity = Math.Max(1, minOrderQuantity.Value);
        if (notes is not null) Notes = notes;
        if (revisionNotes is not null) RevisionNotes = revisionNotes;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public ProductVersion CloneAsRevision(int nextVersionNumber, string? revisionNotes)
    {
        return Create(
            ProductId,
            nextVersionNumber,
            ProductVersionStatuses.Draft,
            SpecificationsJson,
            BomJson,
            OperationsJson,
            AttributesJson,
            ImagesJson,
            CostBreakdownJson,
            TagsJson,
            SellingPrice,
            CostPrice,
            MarginPercent,
            LeadTimeDays,
            MinOrderQuantity,
            Notes,
            revisionNotes ?? $"Revision of v{VersionNumber}");
    }
}
