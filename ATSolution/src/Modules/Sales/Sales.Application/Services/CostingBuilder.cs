using System.Text.Json;
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
        CostingRequest? existing)
    {
        var productLines = new List<object>();
        var materials = new List<object>();
        var coatingItems = new List<object>();
        var lineItems = new List<object>();
        decimal materialCost = 0;
        decimal labourCost = 0;
        decimal machineCost = 0;
        decimal coatingCost = 0;
        decimal overheadCost = 0;

        foreach (var line in order.Lines.OrderBy(l => l.SortOrder))
        {
            var customization = JsonColumn.ParseElement(line.CustomizationJson);
            var sourceType = line.IsCustomized ? "customized" : "standard";
            decimal estimatedCost = 0;
            decimal lineMaterial = 0;
            decimal lineLabour = 0;
            decimal lineMachine = 0;
            decimal lineCoating = 0;
            decimal lineOverhead = 0;

            if (customization.HasValue)
            {
                if (customization.Value.TryGetProperty("estimation", out var estimation)
                    && estimation.TryGetProperty("costBreakdown", out var breakdown))
                {
                    lineMaterial = ReadDecimal(breakdown, "materialCost");
                    lineLabour = ReadDecimal(breakdown, "labourCost");
                    lineMachine = ReadDecimal(breakdown, "machineCost");
                    lineCoating = ReadDecimal(breakdown, "coatingFinishingCost");
                    lineOverhead = ReadDecimal(breakdown, "overheadCost");
                    estimatedCost = lineMaterial + lineLabour + lineMachine + lineCoating + lineOverhead;
                }

                if (customization.Value.TryGetProperty("customizedBom", out var bom)
                    && bom.ValueKind == JsonValueKind.Array)
                {
                    foreach (var bomItem in bom.EnumerateArray())
                    {
                        var qty = ReadDecimal(bomItem, "quantity") * line.Quantity;
                        var waste = ReadDecimal(bomItem, "wastePercent");
                        var unitCost = ReadDecimal(bomItem, "unitCost");
                        var required = Math.Round(qty * (1 + waste / 100m), 4, MidpointRounding.AwayFromZero);
                        var total = Math.Round(required * unitCost, 2, MidpointRounding.AwayFromZero);
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
                            isRequired = bomItem.TryGetProperty("isRequired", out var req) && req.ValueKind == JsonValueKind.True,
                            notes = bomItem.TryGetProperty("notes", out var notes) ? notes.GetString() : null,
                            salesOrderLineItemId = line.Id,
                            sourceType,
                            sourceProductName = line.ProductName,
                            productVersionLabel = line.ProductVersionLabel,
                        });
                    }
                }
            }

            materialCost += lineMaterial * line.Quantity;
            labourCost += lineLabour * line.Quantity;
            machineCost += lineMachine * line.Quantity;
            coatingCost += lineCoating * line.Quantity;
            overheadCost += lineOverhead * line.Quantity;

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
                estimatedCost = estimatedCost > 0 ? estimatedCost : line.UnitPrice * 0.7m,
                materialCost = lineMaterial,
                labourCost = lineLabour,
                machineCost = lineMachine,
                coatingCost = lineCoating,
                overheadCost = lineOverhead,
                customizationId = customization.HasValue && customization.Value.TryGetProperty("id", out var cid)
                    ? cid.GetString()
                    : null,
                customizationStatus = customization.HasValue && customization.Value.TryGetProperty("status", out var st)
                    ? st.GetString()
                    : null,
            });

            coatingItems.Add(new
            {
                id = Guid.NewGuid(),
                productId = line.ProductId,
                productName = line.ProductName,
                finish = "Powder Coating",
                process = "Batch spray",
                quantity = line.Quantity,
                unitCost = lineCoating,
                lineTotal = Math.Round(lineCoating * line.Quantity, 2, MidpointRounding.AwayFromZero),
                salesOrderLineItemId = line.Id,
                sourceType,
                productVersionLabel = line.ProductVersionLabel,
                productSku = line.ProductSku,
            });
        }

        var totalEstimate = Math.Round(materialCost + labourCost + machineCost + coatingCost + overheadCost, 2, MidpointRounding.AwayFromZero);
        if (totalEstimate <= 0)
        {
            totalEstimate = Math.Round(order.TotalAmount * 0.7m, 2, MidpointRounding.AwayFromZero);
        }

        void AddCategory(string category, decimal amount)
        {
            if (amount <= 0) return;
            lineItems.Add(new
            {
                id = Guid.NewGuid(),
                description = category,
                category,
                baseCost = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                percentOfCost = 0m,
            });
        }

        AddCategory("Materials (Components)", materialCost);
        AddCategory("Labour", labourCost);
        AddCategory("Machine", machineCost);
        AddCategory("Coating / Finishing", coatingCost);
        AddCategory("Overhead", overheadCost);

        if (lineItems.Count == 0)
        {
            lineItems.Add(new
            {
                id = Guid.NewGuid(),
                description = "Estimated production cost",
                category = "Production",
                baseCost = totalEstimate,
                percentOfCost = 100m,
            });
        }
        else
        {
            lineItems = lineItems.Select(item =>
            {
                var json = JsonSerializer.SerializeToElement(item, JsonColumn.Options);
                var baseCost = json.GetProperty("baseCost").GetDecimal();
                return (object)new
                {
                    id = json.GetProperty("id").GetGuid(),
                    description = json.GetProperty("description").GetString(),
                    category = json.GetProperty("category").GetString(),
                    baseCost,
                    percentOfCost = totalEstimate > 0
                        ? Math.Round(baseCost / totalEstimate * 100m, 2, MidpointRounding.AwayFromZero)
                        : 0m,
                };
            }).ToList();
        }

        var proposedPrice = order.TotalAmount;
        var marginPercent = proposedPrice > 0
            ? Math.Round((proposedPrice - totalEstimate) / proposedPrice * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var hasBom = materials.Count > 0 || productLines.Count > 0;
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
            ? (hasBom ? CoatingStatuses.Submitted : CoatingStatuses.Skipped)
            : existing!.CoatingStatus;
        var status = autoSubmit
            ? CostingRequestStatuses.InReview
            : existing!.Status;

        var history = autoSubmit || existing is null
            ? new List<object>
            {
                new
                {
                    id = Guid.NewGuid(),
                    action = autoSubmit ? "Estimation generated from sales order" : "Costing created",
                    userName = order.CreatedByName,
                    timestamp = DateTimeOffset.UtcNow,
                    comment = (string?)null,
                },
            }
            : JsonColumn.Deserialize(existing.HistoryJson, new List<object>());

        var approvalLevels = existing is null
            ? new List<object>
            {
                new { id = Guid.NewGuid(), role = "Costing Lead", assigneeName = "Unassigned", status = "pending" },
                new { id = Guid.NewGuid(), role = "Sales Manager", assigneeName = "Unassigned", status = "waiting" },
            }
            : JsonColumn.Deserialize(existing.ApprovalLevelsJson, new List<object>());

        var requester = new CostingRequesterDto(
            order.CreatedByName,
            "Sales",
            order.CustomerEmail,
            GetInitials(order.CreatedByName));

        var configSnapshot = existing?.ConfigSnapshotJson ?? JsonColumn.Serialize(new
        {
            workflowName = "Sales Order Costing",
            version = 1,
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
            configSnapshot);
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

    private static JsonElement ParseArray(string? json) =>
        JsonColumn.ParseElement(json) ?? JsonSerializer.SerializeToElement(Array.Empty<object>(), JsonColumn.Options);

    private static bool IsOpen(string status) =>
        status is SalesOrderStatuses.Draft or SalesOrderStatuses.PendingReview or SalesOrderStatuses.Submitted;

    private static decimal ReadDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.TryGetDecimal(out var value) ? value : 0m;

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) ? prop.GetString() : null;

    private static Guid ReadGuid(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop)
        && prop.ValueKind == JsonValueKind.String
        && Guid.TryParse(prop.GetString(), out var id)
            ? id
            : Guid.Empty;

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "SY";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
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
    string ConfigSnapshotJson);
