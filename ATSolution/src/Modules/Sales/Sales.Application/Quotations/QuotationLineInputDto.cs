using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sales.Application.Quotations;

/// <summary>
/// Line input for create/update. Uses a property bag (not a positional record) so
/// optional JSON blobs like <see cref="Customization"/> can bind as JSON null safely.
/// </summary>
public sealed class QuotationLineInputDto
{
    public Guid? ProductId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ProductVersionId { get; init; }
    public string? ProductVersionLabel { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal TaxPercent { get; init; }
    public bool? IsCustomized { get; init; }
    /// <summary>
    /// Optional customization payload. <see cref="JsonNode"/> accepts JSON null;
    /// <see cref="JsonElement"/> rejects null during ASP.NET model binding.
    /// </summary>
    public JsonNode? Customization { get; init; }
    public bool? RequiresManufacturing { get; init; }

    public string? CustomizationJson =>
        Customization is null || Customization.GetValueKind() is JsonValueKind.Null or JsonValueKind.Undefined
            ? null
            : Customization.ToJsonString();
}
