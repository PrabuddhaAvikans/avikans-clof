namespace Inventory.Application.Reprocessing;

public sealed record ReprocessingCostBreakdownDto(
    decimal Labour,
    decimal Electricity,
    decimal Machine,
    decimal Gas,
    decimal Furnace,
    decimal Subcontract,
    decimal Other);
