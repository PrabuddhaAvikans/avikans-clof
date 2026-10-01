using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record ProductionMaterialOutcomeDto(
    decimal FinishedMaterialQuantity,
    decimal ReusableScrapQuantity,
    decimal RecoverableQuantity,
    decimal PermanentWasteQuantity,
    DateTimeOffset PostedAt,
    IReadOnlyList<string> ScrapLotIds,
    IReadOnlyList<string> RecoverableLotIds);
