using System.Text.Json;
using ATSolution.Application.Abstractions.Workflows;
using Sales.Application.Common;
using Sales.Application.Costing;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.SalesOrders;

namespace Sales.Application.Services;

internal static class CostingBuilder
{
    public static CostingBuildResult BuildFromSalesOrder(
        SalesOrder order,
        string requestNumber,
        CostingRequest? existing,
        Func<Guid?, Guid?, CatalogCostSnapshot?>? resolveCatalog = null,
        CostingApprovalFlowSnapshot? approvalFlow = null)
    {
        var productLines = new List<object>();
        var materials = new List<object>();
        var coatingItems = new List<object>();
        decimal materialCost = 0;
        decimal labourCost = 0;
        decimal machineCost = 0;
        decimal coatingCost = 0;
        decimal overheadCost = 0;

        foreach (var line in order.Lines.OrderBy(l => l.SortOrder))
        {
            var customization = JsonColumn.ParseElement(line.CustomizationJson);
            var catalog = resolveCatalog?.Invoke(line.ProductId, line.ProductVersionId);
            var sourceType = line.IsCustomized ? "customized" : "standard";
            var parts = ReadLineCosts(customization, catalog);
            var unitEstimate = parts.Sum > 0
                ? parts.Sum
                : catalog?.CostPrice ?? 0m;
            if (parts.Sum <= 0 && unitEstimate > 0)
            {
                parts = parts with { Material = unitEstimate };
            }

            var materialExt = RoundMoney(parts.Material * line.Quantity);
            var labourExt = RoundMoney(parts.Labour * line.Quantity);
            var machineExt = RoundMoney(parts.Machine * line.Quantity);
            var coatingExt = RoundMoney(parts.Coating * line.Quantity);
            var overheadExt = RoundMoney((parts.Overhead + parts.Other) * line.Quantity);
            var estimatedExt = materialExt + labourExt + machineExt + coatingExt + overheadExt;

            materialCost += materialExt;
            labourCost += labourExt;
            machineCost += machineExt;
            coatingCost += coatingExt;
            overheadCost += overheadExt;

            AppendMaterials(materials, customization, catalog, line, sourceType);

            var specs = ReadSpecifications(customization, catalog);
            productLines.Add(new
            {
                id = Guid.NewGuid(),
                salesOrderLineItemId = line.Id,
                productId = line.ProductId,
                productSku = line.ProductSku,
                productName = line.ProductName,
                productVersionId = line.ProductVersionId,
                productVersionLabel = line.ProductVersionLabel,
                quantity = line.Quantity,
                sourceType,
                unitPrice = line.UnitPrice,
                sellingAmount = line.LineTotal,
                costsAreExtended = true,
                estimatedCost = estimatedExt,
                materialCost = materialExt,
                labourCost = labourExt,
                machineCost = machineExt,
                coatingCost = coatingExt,
                overheadCost = overheadExt,
                customizationId = customization.HasValue && TryGet(customization.Value, "id", out var cid)
                    ? cid.GetString()
                    : null,
                customizationStatus = customization.HasValue && TryGet(customization.Value, "status", out var st)
                    ? st.GetString()
                    : null,
            });

            coatingItems.Add(new
            {
                id = Guid.NewGuid(),
                productId = line.ProductId,
                productName = line.ProductName,
                finish = ReadString(specs, "coatingFinish") ?? ReadString(specs, "finish") ?? "Powder Coating",
                process = ReadString(specs, "coatingProcess") ?? "Batch spray",
                quantity = line.Quantity,
                unitCost = parts.Coating,
                lineTotal = coatingExt,
                salesOrderLineItemId = line.Id,
                sourceType,
                productVersionLabel = line.ProductVersionLabel,
                productSku = line.ProductSku,
            });
        }

        var totalEstimate = RoundMoney(materialCost + labourCost + machineCost + coatingCost + overheadCost);
        var lineItems = BuildCategories(materialCost, labourCost, machineCost, coatingCost, overheadCost, totalEstimate);
        var proposedPrice = order.TotalAmount;
        var marginPercent = SalesTotals.MarginPercent(proposedPrice, totalEstimate);

        var existingProductLinesEmpty = existing is null
            || string.IsNullOrWhiteSpace(existing.EstimationProductLinesJson)
            || existing.EstimationProductLinesJson.Trim() is "[]";
        var autoSubmit = existing is null
            || (IsOpen(order.Status)
                && existing.Status is not CostingRequestStatuses.Rejected
                    and not CostingRequestStatuses.Approved
                    and not CostingRequestStatuses.ChangesRequested
                && (existing.CoatingStatus == CoatingStatuses.Pending || existingProductLinesEmpty));

        var coatingStatus = autoSubmit
            ? (totalEstimate > 0 ? CoatingStatuses.Submitted : CoatingStatuses.Pending)
            : existing!.CoatingStatus;
        var status = autoSubmit
            ? (totalEstimate > 0 ? CostingRequestStatuses.InReview : CostingRequestStatuses.Pending)
            : existing!.Status;
        if (totalEstimate <= 0)
        {
            coatingStatus = CoatingStatuses.Pending;
            if (status is CostingRequestStatuses.InReview)
            {
                status = CostingRequestStatuses.Pending;
            }
        }

        var history = autoSubmit || existing is null
            ? new List<object>
            {
                new
                {
                    id = Guid.NewGuid(),
                    action = totalEstimate > 0
                        ? "Estimation generated from the product cost sheet"
                        : "Costing created without a product cost sheet",
                    userName = order.CreatedByName,
                    timestamp = DateTimeOffset.UtcNow,
                    comment = totalEstimate > 0
                        ? (string?)null
                        : "Enter material and coating costs before approval. Selling price is not used as a production cost.",
                },
            }
            : JsonColumn.Deserialize(existing.HistoryJson, new List<object>());

        var approvalLevels = existing is null
            ? BuildApprovalLevels(approvalFlow)
            : JsonColumn.Deserialize(existing.ApprovalLevelsJson, new List<object>());

        if (existing is null && approvalLevels.Count == 0 && status == CostingRequestStatuses.InReview)
        {
            status = CostingRequestStatuses.Approved;
            history.Insert(0, new
            {
                id = Guid.NewGuid(),
                action = "Approved",
                userName = "System",
                timestamp = DateTimeOffset.UtcNow,
                comment = "Costing stage has no approval levels.",
            });
        }

        var requester = new CostingRequesterDto(
            order.CreatedByName,
            "Sales",
            order.CustomerEmail,
            GetInitials(order.CreatedByName));

        var configSnapshot = existing?.ConfigSnapshotJson ?? JsonColumn.Serialize(new
        {
            workflowName = approvalFlow?.WorkflowDefinitionName ?? "Sales Order Costing",
            version = approvalFlow?.WorkflowVersionNumber ?? 1,
            capturedAt = DateTimeOffset.UtcNow,
            targetMargin = 25,
        });

        return new CostingBuildResult(
            requestNumber,
            totalEstimate,
            proposedPrice,
            marginPercent,
            JsonColumn.Serialize(lineItems),
            JsonColumn.Serialize(coatingItems),
            JsonColumn.Serialize(materials),
            JsonColumn.Serialize(productLines),
            JsonColumn.Serialize(requester),
            JsonColumn.Serialize(approvalLevels),
            JsonColumn.Serialize(history),
            status,
            coatingStatus,
            configSnapshot,
            approvalFlow?.WorkflowDefinitionId,
            approvalFlow?.WorkflowVersionId,
            null,
            approvalFlow?.WorkflowVersionNumber,
            approvalFlow?.WorkflowDefinitionName);
    }

    private static List<object> BuildApprovalLevels(CostingApprovalFlowSnapshot? approvalFlow)
    {
        if (approvalFlow is null)
        {
            return
            [
                new { id = Guid.NewGuid(), role = "Costing Lead", assigneeName = "Unassigned", status = "pending" },
                new { id = Guid.NewGuid(), role = "Sales Manager", assigneeName = "Unassigned", status = "waiting" },
            ];
        }

        if (approvalFlow.Levels.Count == 0)
        {
            return [];
        }

        return approvalFlow.Levels
            .Select(level => (object)new
            {
                id = Guid.TryParse(level.Id, out var parsed) ? parsed : Guid.NewGuid(),
                role = level.Role,
                assigneeName = level.AssigneeName,
                status = level.Status,
            })
            .ToList();
    }

    public static bool IsSellingPricePlaceholder(CostingRequest entity)
    {
        if (entity.Status is CostingRequestStatuses.Approved or CostingRequestStatuses.Rejected)
        {
            return false;
        }

        if (entity.ProposedPrice <= 0 || entity.TotalEstimate <= 0)
        {
            return false;
        }

        var fallback = RoundMoney(entity.ProposedPrice * 0.7m);
        if (entity.TotalEstimate != fallback)
        {
            return false;
        }

        var element = JsonColumn.ParseElement(entity.LineItemsJson);
        if (element is null || element.Value.ValueKind != JsonValueKind.Array || element.Value.GetArrayLength() != 1)
        {
            return false;
        }

        return string.Equals(ReadString(element.Value[0], "category"), "Production", StringComparison.OrdinalIgnoreCase);
    }

    public static SubmittedEstimation ApplySubmission(
        CostingRequest entity,
        IReadOnlyList<CoatingSubmitItemDto> items,
        IReadOnlyList<EstimationMaterialInputDto>? materials)
    {
        var coatingItems = items.Select(item =>
        {
            var lineTotal = RoundMoney(item.UnitCost * item.Quantity);
            return new
            {
                id = item.Id ?? Guid.NewGuid(),
                productId = item.ProductId,
                productName = item.ProductName,
                finish = item.Finish,
                process = item.Process,
                quantity = item.Quantity,
                unitCost = item.UnitCost,
                lineTotal,
                salesOrderLineItemId = item.SalesOrderLineItemId,
                sourceType = item.SourceType,
                productVersionLabel = item.ProductVersionLabel,
                productSku = item.ProductSku,
            };
        }).ToList();

        string materialsJson = entity.EstimationMaterialsJson;
        Dictionary<Guid, decimal>? materialByLine = null;
        if (materials is { Count: > 0 })
        {
            var materialRows = materials.Select(mat =>
            {
                var required = Math.Round(mat.Quantity * (1 + mat.WastePercent / 100m), 4, MidpointRounding.AwayFromZero);
                var totalCost = RoundMoney(required * mat.UnitCost);
                return new
                {
                    id = mat.Id ?? Guid.NewGuid(),
                    inventoryItemId = mat.InventoryItemId,
                    inventoryItemName = mat.InventoryItemName,
                    sku = mat.Sku,
                    quantity = mat.Quantity,
                    unit = mat.Unit,
                    wastePercent = mat.WastePercent,
                    requiredQuantity = required,
                    unitCost = mat.UnitCost,
                    totalCost,
                    isRequired = mat.IsRequired,
                    alternativeItemId = mat.AlternativeItemId,
                    alternativeItemName = mat.AlternativeItemName,
                    notes = mat.Notes,
                    salesOrderLineItemId = mat.SalesOrderLineItemId,
                    sourceType = mat.SourceType,
                    sourceProductName = mat.SourceProductName,
                    productVersionLabel = mat.ProductVersionLabel,
                };
            }).ToList();
            materialsJson = JsonColumn.Serialize(materialRows);
            materialByLine = materialRows
                .Where(row => row.salesOrderLineItemId.HasValue)
                .GroupBy(row => row.salesOrderLineItemId!.Value)
                .ToDictionary(group => group.Key, group => group.Sum(row => row.totalCost));
        }

        var coatingByLine = coatingItems
            .Where(item => item.salesOrderLineItemId.HasValue)
            .GroupBy(item => item.salesOrderLineItemId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.lineTotal));

        var productElements = JsonColumn.ParseElement(entity.EstimationProductLinesJson);
        var updatedLines = new List<object>();
        decimal materialCost = 0;
        decimal labourCost = 0;
        decimal machineCost = 0;
        decimal coatingCost = 0;
        decimal overheadCost = 0;

        if (productElements is { ValueKind: JsonValueKind.Array })
        {
            foreach (var line in productElements.Value.EnumerateArray())
            {
                var quantity = ReadDecimal(line, "quantity");
                var material = ReadDecimal(line, "materialCost");
                var labour = ReadDecimal(line, "labourCost");
                var machine = ReadDecimal(line, "machineCost");
                var coating = ReadDecimal(line, "coatingCost");
                var overhead = ReadDecimal(line, "overheadCost");
                var estimated = ReadDecimal(line, "estimatedCost");
                var alreadyExtended = TryGet(line, "costsAreExtended", out var extendedFlag)
                    && extendedFlag.ValueKind == JsonValueKind.True;
                var componentSum = material + labour + machine + coating + overhead;
                if (!alreadyExtended && quantity > 1 && componentSum > 0 && Math.Abs(componentSum - estimated) < 0.05m)
                {
                    material = RoundMoney(material * quantity);
                    labour = RoundMoney(labour * quantity);
                    machine = RoundMoney(machine * quantity);
                    coating = RoundMoney(coating * quantity);
                    overhead = RoundMoney(overhead * quantity);
                }

                var lineId = ReadGuid(line, "salesOrderLineItemId");
                if (materialByLine is not null && materialByLine.TryGetValue(lineId, out var submittedMaterial))
                {
                    material = submittedMaterial;
                }

                if (coatingByLine.TryGetValue(lineId, out var submittedCoating))
                {
                    coating = submittedCoating;
                }

                estimated = material + labour + machine + coating + overhead;
                materialCost += material;
                labourCost += labour;
                machineCost += machine;
                coatingCost += coating;
                overheadCost += overhead;

                updatedLines.Add(new
                {
                    id = TryGet(line, "id", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var parsed)
                        ? parsed
                        : Guid.NewGuid(),
                    salesOrderLineItemId = lineId,
                    productId = ReadNullableGuid(line, "productId"),
                    productSku = ReadString(line, "productSku"),
                    productName = ReadString(line, "productName"),
                    productVersionId = ReadNullableGuid(line, "productVersionId"),
                    productVersionLabel = ReadString(line, "productVersionLabel"),
                    quantity,
                    sourceType = ReadString(line, "sourceType") ?? "standard",
                    unitPrice = ReadDecimal(line, "unitPrice"),
                    sellingAmount = ReadDecimal(line, "sellingAmount") > 0
                        ? ReadDecimal(line, "sellingAmount")
                        : RoundMoney(ReadDecimal(line, "unitPrice") * quantity),
                    costsAreExtended = true,
                    estimatedCost = estimated,
                    materialCost = material,
                    labourCost = labour,
                    machineCost = machine,
                    coatingCost = coating,
                    overheadCost = overhead,
                    customizationId = ReadString(line, "customizationId"),
                    customizationStatus = ReadString(line, "customizationStatus"),
                });
            }
        }

        if (updatedLines.Count == 0)
        {
            materialCost = materialByLine?.Values.Sum() ?? 0;
            coatingCost = coatingItems.Sum(item => item.lineTotal);
        }

        var totalEstimate = RoundMoney(materialCost + labourCost + machineCost + coatingCost + overheadCost);
        var lineItems = BuildCategories(materialCost, labourCost, machineCost, coatingCost, overheadCost, totalEstimate);
        var marginPercent = SalesTotals.MarginPercent(entity.ProposedPrice, totalEstimate);

        return new SubmittedEstimation(
            JsonColumn.Serialize(coatingItems),
            materialsJson,
            JsonColumn.Serialize(lineItems),
            updatedLines.Count > 0 ? JsonColumn.Serialize(updatedLines) : entity.EstimationProductLinesJson,
            totalEstimate,
            marginPercent);
    }

    public static CostingRequestDto Map(CostingRequest entity)
    {
        var requester = JsonColumn.Deserialize(
            entity.RequesterJson,
            new CostingRequesterDto("System", "System", "", "SY"));

        return new CostingRequestDto(
            entity.Id,
            entity.Number,
            entity.CustomerName,
            entity.ProjectName,
            entity.RequestType,
            entity.RequestedDateUtc,
            entity.TotalEstimate,
            entity.ProposedPrice,
            entity.MarginPercent,
            entity.TargetMargin,
            entity.RiskFlag,
            entity.SlaRemaining,
            entity.Status,
            entity.CoatingStatus,
            entity.Currency,
            entity.PaymentTerms,
            ParseArray(entity.LineItemsJson),
            ParseArray(entity.CoatingItemsJson),
            ParseArray(entity.EstimationMaterialsJson),
            ParseArray(entity.EstimationProductLinesJson),
            ParseArray(entity.AttachmentsJson),
            entity.Notes,
            requester,
            JsonColumn.Deserialize(entity.ApprovalLevelsJson, Array.Empty<ApprovalLevelDto>()),
            JsonColumn.Deserialize(entity.HistoryJson, Array.Empty<ApprovalHistoryEntryDto>()),
            entity.SalesOrderId,
            entity.SalesOrderNumber,
            entity.QuotationId,
            entity.QuotationNumber,
            entity.WorkflowDefinitionId,
            entity.WorkflowVersionId,
            entity.WorkflowInstanceId,
            entity.WorkflowVersionNumber,
            entity.WorkflowName);
    }

    private static List<object> BuildCategories(
        decimal materialCost,
        decimal labourCost,
        decimal machineCost,
        decimal coatingCost,
        decimal overheadCost,
        decimal totalEstimate)
    {
        var lineItems = new List<object>();
        void AddCategory(string category, decimal amount)
        {
            if (amount <= 0) return;
            lineItems.Add(new
            {
                id = Guid.NewGuid(),
                description = category,
                category,
                baseCost = RoundMoney(amount),
                percentOfCost = totalEstimate > 0
                    ? RoundMoney(amount / totalEstimate * 100m)
                    : 0m,
            });
        }

        AddCategory("Materials (Components)", materialCost);
        AddCategory("Labour", labourCost);
        AddCategory("Machine", machineCost);
        AddCategory("Coating / Finishing", coatingCost);
        AddCategory("Overhead", overheadCost);
        return lineItems;
    }

    private static void AppendMaterials(
        List<object> materials,
        JsonElement? customization,
        CatalogCostSnapshot? catalog,
        SalesOrderLine line,
        string sourceType)
    {
        JsonElement? bom = null;
        if (customization.HasValue && TryGet(customization.Value, "customizedBom", out var customizedBom)
            && customizedBom.ValueKind == JsonValueKind.Array
            && customizedBom.GetArrayLength() > 0)
        {
            bom = customizedBom;
        }
        else if (!string.IsNullOrWhiteSpace(catalog?.BomJson))
        {
            var parsed = JsonColumn.ParseElement(catalog.Value.BomJson);
            if (parsed is { ValueKind: JsonValueKind.Array } && parsed.Value.GetArrayLength() > 0)
            {
                bom = parsed;
            }
        }

        if (bom is null) return;

        foreach (var bomItem in bom.Value.EnumerateArray())
        {
            var qty = ReadDecimal(bomItem, "quantity") * line.Quantity;
            var waste = ReadDecimal(bomItem, "wastePercent");
            var unitCost = ReadDecimal(bomItem, "unitCost");
            var required = Math.Round(qty * (1 + waste / 100m), 4, MidpointRounding.AwayFromZero);
            var total = RoundMoney(required * unitCost);
            materials.Add(new
            {
                id = Guid.NewGuid(),
                inventoryItemId = ReadGuid(bomItem, "inventoryItemId"),
                inventoryItemName = ReadString(bomItem, "inventoryItemName"),
                sku = ReadString(bomItem, "sku"),
                quantity = qty,
                unit = ReadString(bomItem, "unit") ?? "ea",
                wastePercent = waste,
                requiredQuantity = required,
                unitCost,
                totalCost = total,
                isRequired = TryGet(bomItem, "isRequired", out var req) && req.ValueKind == JsonValueKind.True,
                notes = TryGet(bomItem, "notes", out var notes) ? notes.GetString() : null,
                salesOrderLineItemId = line.Id,
                sourceType,
                sourceProductName = line.ProductName,
                productVersionLabel = line.ProductVersionLabel,
            });
        }
    }

    private static UnitCostParts ReadLineCosts(JsonElement? customization, CatalogCostSnapshot? catalog)
    {
        var parts = default(UnitCostParts);
        if (customization.HasValue
            && TryGet(customization.Value, "estimation", out var estimation)
            && TryGet(estimation, "costBreakdown", out var breakdown))
        {
            parts = ReadBreakdown(breakdown);
        }

        if (parts.Sum <= 0
            && customization.HasValue
            && TryGet(customization.Value, "base", out var baseSnapshot)
            && TryGet(baseSnapshot, "costBreakdown", out var baseBreakdown))
        {
            parts = ReadBreakdown(baseBreakdown);
        }

        if (parts.Sum <= 0 && !string.IsNullOrWhiteSpace(catalog?.CostBreakdownJson))
        {
            var catalogBreakdown = JsonColumn.ParseElement(catalog.Value.CostBreakdownJson);
            if (catalogBreakdown.HasValue)
            {
                parts = ReadBreakdown(catalogBreakdown.Value);
            }
        }

        return parts;
    }

    private static UnitCostParts ReadBreakdown(JsonElement breakdown)
    {
        return new UnitCostParts(
            ReadDecimal(breakdown, "materialCost"),
            ReadDecimal(breakdown, "labourCost"),
            ReadDecimal(breakdown, "machineCost"),
            ReadDecimal(breakdown, "coatingFinishingCost"),
            ReadDecimal(breakdown, "overheadCost"),
            ReadDecimal(breakdown, "otherCost") + ReadExtraLines(breakdown));
    }

    private static JsonElement ReadSpecifications(JsonElement? customization, CatalogCostSnapshot? catalog)
    {
        if (customization.HasValue && TryGet(customization.Value, "customizedSpecifications", out var specs))
        {
            return specs;
        }

        return JsonColumn.ParseElement(catalog?.SpecificationsJson) ?? default;
    }

    private static decimal ReadExtraLines(JsonElement breakdown)
    {
        if (!TryGet(breakdown, "extraLines", out var lines) || lines.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        decimal sum = 0;
        foreach (var line in lines.EnumerateArray())
        {
            sum += ReadDecimal(line, "amount");
        }

        return sum;
    }

    private static JsonElement ParseArray(string? json) =>
        JsonColumn.ParseElement(json) ?? JsonSerializer.SerializeToElement(Array.Empty<object>(), JsonColumn.Options);

    private static bool IsOpen(string status) =>
        status is SalesOrderStatuses.Draft or SalesOrderStatuses.PendingReview or SalesOrderStatuses.Submitted;

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool TryGet(JsonElement element, string name, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(name, out property)) return true;
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static decimal ReadDecimal(JsonElement element, string name) =>
        TryGet(element, name, out var prop) && prop.TryGetDecimal(out var value) ? value : 0m;

    private static string? ReadString(JsonElement element, string name) =>
        TryGet(element, name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;

    private static Guid ReadGuid(JsonElement element, string name) =>
        TryGet(element, name, out var prop)
        && prop.ValueKind == JsonValueKind.String
        && Guid.TryParse(prop.GetString(), out var id)
            ? id
            : Guid.Empty;

    private static Guid? ReadNullableGuid(JsonElement element, string name)
    {
        var id = ReadGuid(element, name);
        return id == Guid.Empty ? null : id;
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "SY";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private readonly record struct UnitCostParts(
        decimal Material,
        decimal Labour,
        decimal Machine,
        decimal Coating,
        decimal Overhead,
        decimal Other)
    {
        public decimal Sum => Material + Labour + Machine + Coating + Overhead + Other;
    }
}

internal sealed record CostingBuildResult(
    string RequestNumber,
    decimal TotalEstimate,
    decimal ProposedPrice,
    decimal MarginPercent,
    string LineItemsJson,
    string CoatingItemsJson,
    string EstimationMaterialsJson,
    string EstimationProductLinesJson,
    string RequesterJson,
    string ApprovalLevelsJson,
    string HistoryJson,
    string Status,
    string CoatingStatus,
    string ConfigSnapshotJson,
    string? WorkflowDefinitionId = null,
    string? WorkflowVersionId = null,
    string? WorkflowInstanceId = null,
    int? WorkflowVersionNumber = null,
    string? WorkflowName = null);

internal sealed record SubmittedEstimation(
    string CoatingItemsJson,
    string MaterialsJson,
    string LineItemsJson,
    string ProductLinesJson,
    decimal TotalEstimate,
    decimal MarginPercent);
