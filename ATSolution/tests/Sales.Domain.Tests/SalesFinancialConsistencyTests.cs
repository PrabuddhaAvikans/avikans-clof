using System.Text.Json;
using Sales.Application.Common;
using Sales.Application.Costing;
using Sales.Application.Services;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.SalesOrders;

namespace Sales.Domain.Tests;

public class SalesFinancialConsistencyTests
{
    [Fact]
    public void Margin_on_the_example_order_is_thirty_percent_of_selling_price()
    {
        var margin = SalesTotals.MarginPercent(26550m, 18585m);
        Assert.Equal(30m, margin);
        Assert.Equal(7965m, 26550m - 18585m);
    }

    [Fact]
    public void Header_discount_reduces_the_taxable_amount()
    {
        var totals = SalesTotals.Compute(
            [(Quantity: 1m, UnitPrice: 1000m, DiscountPercent: 0m, TaxPercent: 10m)],
            discountAmount: 100m);

        Assert.Equal(1000m, totals.Subtotal);
        Assert.Equal(90m, totals.TaxAmount);
        Assert.Equal(990m, totals.TotalAmount);
    }

    [Fact]
    public void Costing_uses_the_quotation_cost_sheet_instead_of_seventy_percent_of_price()
    {
        var order = OrderWithLine(
            unitPrice: 26550m,
            quantity: 2m,
            lineTotal: 53100m,
            orderTotal: 53100m,
            customizationJson: """
            {
              "estimation": {
                "costBreakdown": {
                  "materialCost": 100,
                  "labourCost": 20,
                  "machineCost": 10,
                  "coatingFinishingCost": 150,
                  "overheadCost": 5,
                  "otherCost": 10,
                  "extraLines": [{ "amount": 5 }]
                }
              },
              "customizedSpecifications": { "coatingFinish": "Powder Coating" }
            }
            """);

        var built = CostingBuilder.BuildFromSalesOrder(order, "CR-2026-0002", null);

        Assert.Equal(600m, built.TotalEstimate);
        Assert.Equal(53100m, built.ProposedPrice);
        Assert.NotEqual(Math.Round(53100m * 0.7m, 2), built.TotalEstimate);

        using var coating = JsonDocument.Parse(built.CoatingItemsJson);
        var item = coating.RootElement[0];
        Assert.Equal(150m, item.GetProperty("unitCost").GetDecimal());
        Assert.Equal(300m, item.GetProperty("lineTotal").GetDecimal());
        Assert.Equal("Powder Coating", item.GetProperty("finish").GetString());
    }

    [Fact]
    public void Standard_lines_use_the_catalog_cost_sheet_and_keep_coating()
    {
        var order = OrderWithLine(
            unitPrice: 26550m,
            quantity: 1m,
            lineTotal: 26550m,
            orderTotal: 26550m,
            customizationJson: null);

        var built = CostingBuilder.BuildFromSalesOrder(
            order,
            "CR-2026-0002",
            null,
            (_, _) => new CatalogCostSnapshot(
                """{"finish":"Anodized","coatingProcess":"Dip"}""",
                "[]",
                """{"materialCost":200,"labourCost":0,"machineCost":0,"coatingFinishingCost":40,"overheadCost":0,"otherCost":0}""",
                CostPrice: 999m));

        Assert.Equal(240m, built.TotalEstimate);
        Assert.NotEqual(18585m, built.TotalEstimate);

        using var coating = JsonDocument.Parse(built.CoatingItemsJson);
        Assert.Equal(40m, coating.RootElement[0].GetProperty("unitCost").GetDecimal());
        Assert.Equal("Anodized", coating.RootElement[0].GetProperty("finish").GetString());
    }

    [Fact]
    public void Missing_cost_sheet_does_not_invent_a_margin_from_selling_price()
    {
        var order = OrderWithLine(26550m, 1m, 26550m, 26550m, null);
        var built = CostingBuilder.BuildFromSalesOrder(order, "CR-2026-0002", null);

        Assert.Equal(0m, built.TotalEstimate);
        Assert.Equal(100m, built.MarginPercent);
        Assert.Equal(CostingRequestStatuses.Pending, built.Status);
        Assert.Equal(CoatingStatuses.Pending, built.CoatingStatus);
    }

    [Fact]
    public void Catalog_cost_price_is_used_when_the_cost_sheet_is_empty()
    {
        var order = OrderWithLine(26550m, 3m, 79650m, 79650m, null);
        var built = CostingBuilder.BuildFromSalesOrder(
            order,
            "CR-1",
            null,
            (_, _) => new CatalogCostSnapshot(null, "[]", """{"materialCost":0}""", 100m));

        Assert.Equal(300m, built.TotalEstimate);
    }

    [Fact]
    public void Placeholder_detection_matches_the_old_seventy_percent_estimate()
    {
        var request = CostingRequest.Create(
            "CR-2026-0002",
            Guid.NewGuid(),
            "SO-2026-0002",
            Guid.NewGuid(),
            "Q-2026-0003",
            "Customer",
            "SO SO-2026-0002",
            18585m,
            26550m,
            30m,
            "LKR",
            "Net 30",
            """[{"id":"11111111-1111-1111-1111-111111111111","description":"Estimated production cost","category":"Production","baseCost":18585,"percentOfCost":100}]""",
            "[]",
            "[]",
            "[]",
            "{}",
            "[]",
            "[]",
            CostingRequestStatuses.InReview,
            CoatingStatuses.Submitted,
            null);

        Assert.True(CostingBuilder.IsSellingPricePlaceholder(request));
    }

    [Fact]
    public void Submitted_estimation_keeps_labour_and_replaces_coating()
    {
        var order = OrderWithLine(1000m, 2m, 2000m, 2000m, """
        {
          "estimation": {
            "costBreakdown": {
              "materialCost": 100,
              "labourCost": 50,
              "machineCost": 0,
              "coatingFinishingCost": 0,
              "overheadCost": 10,
              "otherCost": 0
            }
          }
        }
        """);
        var built = CostingBuilder.BuildFromSalesOrder(order, "CR-1", null);
        var request = CostingRequest.Create(
            built.RequestNumber,
            order.Id,
            order.Number,
            null,
            null,
            order.CustomerName,
            "Project",
            built.TotalEstimate,
            built.ProposedPrice,
            built.MarginPercent,
            "LKR",
            "Net 30",
            built.LineItemsJson,
            built.CoatingItemsJson,
            built.EstimationMaterialsJson,
            built.EstimationProductLinesJson,
            built.RequesterJson,
            built.ApprovalLevelsJson,
            built.HistoryJson,
            built.Status,
            built.CoatingStatus,
            built.ConfigSnapshotJson);

        using var lines = JsonDocument.Parse(built.EstimationProductLinesJson);
        var lineId = lines.RootElement[0].GetProperty("salesOrderLineItemId").GetGuid();
        var submitted = CostingBuilder.ApplySubmission(
            request,
            [
                new CoatingSubmitItemDto(
                    null,
                    null,
                    "Panel",
                    "Powder Coating",
                    "Batch spray",
                    2m,
                    25m,
                    lineId),
            ],
            null);

        Assert.Equal(370m, submitted.TotalEstimate);
        using var updated = JsonDocument.Parse(submitted.ProductLinesJson);
        Assert.Equal(100m, updated.RootElement[0].GetProperty("labourCost").GetDecimal());
        Assert.Equal(50m, updated.RootElement[0].GetProperty("coatingCost").GetDecimal());
    }

    private static SalesOrder OrderWithLine(
        decimal unitPrice,
        decimal quantity,
        decimal lineTotal,
        decimal orderTotal,
        string? customizationJson)
    {
        var order = SalesOrder.Create(
            "SO-2026-0002",
            Guid.NewGuid(),
            "Customer",
            "customer@example.com",
            Guid.NewGuid(),
            "Q-2026-0003",
            PriorityValues.Medium,
            0m,
            orderTotal,
            0m,
            orderTotal,
            "LKR",
            "{}",
            null,
            null,
            null,
            null,
            "usr",
            "Test User");
        order.Lines.Add(SalesOrderLine.Create(
            order.Id,
            Guid.NewGuid(),
            "SKU-1",
            "Panel",
            Guid.NewGuid(),
            "v1",
            null,
            quantity,
            unitPrice,
            0m,
            0m,
            lineTotal,
            customizationJson is not null,
            true,
            customizationJson,
            0));
        return order;
    }
}
