using System.Text.Json;
using Sales.Application.Quotations;
using Sales.Application.SalesOrders;

namespace Sales.Domain.Tests;

public class LineInputCustomizationBindingTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Sales_order_line_accepts_null_customization()
    {
        const string json = """
            {
              "productId": "11111111-1111-1111-1111-111111111111",
              "productSku": "SKU-1",
              "productName": "Panel",
              "quantity": 1,
              "unitPrice": 100,
              "discountPercent": 0,
              "taxPercent": 0,
              "isCustomized": false,
              "customization": null,
              "requiresManufacturing": true
            }
            """;

        var line = JsonSerializer.Deserialize<SalesOrderLineInputDto>(json, Options);

        Assert.NotNull(line);
        Assert.Null(line!.Customization);
        Assert.Null(line.CustomizationJson);
        Assert.Equal("SKU-1", line.ProductSku);
    }

    [Fact]
    public void Quotation_line_accepts_null_customization()
    {
        const string json = """
            {
              "productSku": "SKU-1",
              "productName": "Panel",
              "quantity": 2,
              "unitPrice": 50,
              "customization": null
            }
            """;

        var line = JsonSerializer.Deserialize<QuotationLineInputDto>(json, Options);

        Assert.NotNull(line);
        Assert.Null(line!.Customization);
        Assert.Null(line.CustomizationJson);
    }

    [Fact]
    public void Sales_order_line_keeps_customization_object()
    {
        const string json = """
            {
              "productSku": "SKU-1",
              "productName": "Panel",
              "quantity": 1,
              "unitPrice": 100,
              "customization": { "id": "qpc-1", "status": "approved" }
            }
            """;

        var line = JsonSerializer.Deserialize<SalesOrderLineInputDto>(json, Options);

        Assert.NotNull(line?.Customization);
        Assert.Contains("qpc-1", line!.CustomizationJson);
    }
}
