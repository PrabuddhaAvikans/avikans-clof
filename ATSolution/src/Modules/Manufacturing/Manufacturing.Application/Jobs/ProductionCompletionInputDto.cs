using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record ProductionCompletionInputDto(
    decimal FinishedMaterialQuantity,
    decimal ReusableScrapQuantity,
    decimal? RecoverableQuantity,
    decimal PermanentWasteQuantity,
    string? Notes);
