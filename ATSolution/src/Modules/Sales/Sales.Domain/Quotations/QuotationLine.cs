using ATSolution.Domain.Entities.Common;

namespace Sales.Domain.Quotations;

public class QuotationLine : Entity<Guid>
{
    public Guid QuotationId { get; private set; }
    public Quotation Quotation { get; private set; } = null!;
    public Guid? ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Guid? ProductVersionId { get; private set; }
    public string? ProductVersionLabel { get; private set; }
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }
    public bool IsCustomized { get; private set; }
    public bool RequiresManufacturing { get; private set; }
    public string? CustomizationJson { get; private set; }
    public int SortOrder { get; private set; }

    public static QuotationLine Create(
        Guid quotationId,
        Guid? productId,
        string productSku,
        string productName,
        Guid? productVersionId,
        string? productVersionLabel,
        string? description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        decimal lineTotal,
        bool isCustomized,
        bool requiresManufacturing,
        string? customizationJson,
        int sortOrder)
    {
        return new QuotationLine
        {
            Id = Guid.NewGuid(),
            QuotationId = quotationId,
            ProductId = productId,
            ProductSku = productSku,
            ProductName = productName,
            ProductVersionId = productVersionId,
            ProductVersionLabel = productVersionLabel,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercent = discountPercent,
            TaxPercent = taxPercent,
            LineTotal = lineTotal,
            IsCustomized = isCustomized,
            RequiresManufacturing = requiresManufacturing,
            CustomizationJson = customizationJson,
            SortOrder = sortOrder,
        };
    }

    public void UpdateCustomization(string? customizationJson, bool isCustomized)
    {
        CustomizationJson = customizationJson;
        IsCustomized = isCustomized;
    }

    public void LockCustomization(string? customizationJson)
    {
        CustomizationJson = customizationJson;
        IsCustomized = !string.IsNullOrWhiteSpace(customizationJson);
    }
}
