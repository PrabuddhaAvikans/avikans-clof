using ATSolution.Domain.Entities.Common;
using Inventory.Domain.Common;

namespace Inventory.Domain.Reprocessing;

public class ReprocessingBatch : Entity<Guid>, IAuditableEntity
{
    public string BatchNumber { get; private set; } = null!;
    public string Status { get; private set; } = ReprocessingBatchStatuses.Draft;
    public Guid InputScrapLotId { get; private set; }
    public string InputScrapSku { get; private set; } = null!;
    public string InputScrapName { get; private set; } = null!;
    public decimal InputQuantity { get; private set; }
    public string InputUnit { get; private set; } = null!;
    public decimal InputUnitCost { get; private set; }
    public Guid? WipLotId { get; private set; }
    public Guid? IssueMovementId { get; private set; }
    public decimal CostLabour { get; private set; }
    public decimal CostElectricity { get; private set; }
    public decimal CostMachine { get; private set; }
    public decimal CostGas { get; private set; }
    public decimal CostFurnace { get; private set; }
    public decimal CostSubcontract { get; private set; }
    public decimal CostOther { get; private set; }
    public decimal TotalProcessingCost { get; private set; }
    public decimal? RecoveredQuantity { get; private set; }
    public decimal? ProcessLossQuantity { get; private set; }
    public decimal? RecoveredUnitCost { get; private set; }
    public Guid? RecoveredLotId { get; private set; }
    public string? RecoveredLotSku { get; private set; }
    public string? SourceProductionOrderId { get; private set; }
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public string CreatedByName { get; private set; } = null!;
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static ReprocessingBatch Create(
        string batchNumber,
        Guid inputScrapLotId,
        string inputScrapSku,
        string inputScrapName,
        decimal inputQuantity,
        string inputUnit,
        decimal inputUnitCost,
        decimal costLabour,
        decimal costElectricity,
        decimal costMachine,
        decimal costGas,
        decimal costFurnace,
        decimal costSubcontract,
        decimal costOther,
        string? sourceProductionOrderId,
        string? notes,
        string createdBy,
        string createdByName)
    {
        if (inputQuantity <= 0)
        {
            throw new InvalidOperationException("Input quantity must be greater than zero.");
        }

        var now = DateTimeOffset.UtcNow;
        var batch = new ReprocessingBatch
        {
            Id = Guid.NewGuid(),
            BatchNumber = batchNumber.Trim(),
            Status = ReprocessingBatchStatuses.Draft,
            InputScrapLotId = inputScrapLotId,
            InputScrapSku = inputScrapSku.Trim(),
            InputScrapName = inputScrapName.Trim(),
            InputQuantity = inputQuantity,
            InputUnit = inputUnit.Trim(),
            InputUnitCost = inputUnitCost,
            SourceProductionOrderId = string.IsNullOrWhiteSpace(sourceProductionOrderId)
                ? null
                : sourceProductionOrderId.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedBy = createdBy.Trim(),
            CreatedByName = createdByName.Trim(),
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };

        batch.SetCosts(
            costLabour,
            costElectricity,
            costMachine,
            costGas,
            costFurnace,
            costSubcontract,
            costOther);

        return batch;
    }

    public void Start(Guid? issueMovementId = null, Guid? wipLotId = null)
    {
        if (Status == ReprocessingBatchStatuses.Completed)
        {
            throw new InvalidOperationException("Completed reprocessing batches cannot be started again.");
        }

        if (Status == ReprocessingBatchStatuses.Cancelled)
        {
            throw new InvalidOperationException("Cancelled reprocessing batches cannot be started.");
        }

        if (Status == ReprocessingBatchStatuses.InProgress)
        {
            return;
        }

        Status = ReprocessingBatchStatuses.InProgress;
        StartedAtUtc = DateTimeOffset.UtcNow;
        if (issueMovementId.HasValue)
        {
            IssueMovementId = issueMovementId;
        }

        if (wipLotId.HasValue)
        {
            WipLotId = wipLotId;
        }

        ModifiedOnUtc = StartedAtUtc.Value;
    }

    public void Complete(
        decimal recoveredQuantity,
        decimal processLossQuantity,
        decimal? costLabour,
        decimal? costElectricity,
        decimal? costMachine,
        decimal? costGas,
        decimal? costFurnace,
        decimal? costSubcontract,
        decimal? costOther,
        Guid? recoveredLotId,
        string? recoveredLotSku,
        string? notes)
    {
        if (Status == ReprocessingBatchStatuses.Completed)
        {
            return;
        }

        if (Status == ReprocessingBatchStatuses.Cancelled)
        {
            throw new InvalidOperationException("Cancelled reprocessing batches cannot be completed.");
        }

        if (Status == ReprocessingBatchStatuses.Draft)
        {
            Start();
        }

        if (recoveredQuantity <= 0)
        {
            throw new InvalidOperationException("Recovered quantity must be greater than zero.");
        }

        if (processLossQuantity < 0 || recoveredQuantity < 0)
        {
            throw new InvalidOperationException("Recovered and process-loss quantities cannot be negative.");
        }

        if (recoveredQuantity > InputQuantity + 0.0001m)
        {
            throw new InvalidOperationException("Recovered quantity cannot exceed reprocessing input quantity.");
        }

        var accounted = recoveredQuantity + processLossQuantity;
        var difference = Math.Abs(InputQuantity - accounted);
        if (difference > 0.0001m)
        {
            throw new InvalidOperationException(
                $"Input ({InputQuantity}) must equal recovered + process loss ({accounted}). Difference: {InputQuantity - accounted}.");
        }

        SetCosts(
            costLabour ?? CostLabour,
            costElectricity ?? CostElectricity,
            costMachine ?? CostMachine,
            costGas ?? CostGas,
            costFurnace ?? CostFurnace,
            costSubcontract ?? CostSubcontract,
            costOther ?? CostOther);

        RecoveredQuantity = recoveredQuantity;
        ProcessLossQuantity = processLossQuantity;
        RecoveredUnitCost = Math.Round(
            ((InputQuantity * InputUnitCost) + TotalProcessingCost) / recoveredQuantity,
            4);
        RecoveredLotId = recoveredLotId;
        RecoveredLotSku = string.IsNullOrWhiteSpace(recoveredLotSku) ? null : recoveredLotSku.Trim();

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? notes.Trim()
                : $"{Notes}\n{notes.Trim()}";
        }

        Status = ReprocessingBatchStatuses.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = CompletedAtUtc.Value;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == ReprocessingBatchStatuses.Completed)
        {
            throw new InvalidOperationException("Completed reprocessing batches cannot be cancelled.");
        }

        if (Status == ReprocessingBatchStatuses.InProgress && IssueMovementId.HasValue)
        {
            throw new InvalidOperationException(
                "In-progress batches that already issued scrap cannot be cancelled.");
        }

        if (Status == ReprocessingBatchStatuses.Cancelled)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var cancelNote = $"Cancelled: {reason.Trim()}";
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? cancelNote
                : $"{Notes}\n{cancelNote}";
        }

        Status = ReprocessingBatchStatuses.Cancelled;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private void SetCosts(
        decimal labour,
        decimal electricity,
        decimal machine,
        decimal gas,
        decimal furnace,
        decimal subcontract,
        decimal other)
    {
        CostLabour = Math.Max(0, labour);
        CostElectricity = Math.Max(0, electricity);
        CostMachine = Math.Max(0, machine);
        CostGas = Math.Max(0, gas);
        CostFurnace = Math.Max(0, furnace);
        CostSubcontract = Math.Max(0, subcontract);
        CostOther = Math.Max(0, other);
        TotalProcessingCost = Math.Round(
            CostLabour + CostElectricity + CostMachine + CostGas + CostFurnace + CostSubcontract + CostOther,
            4);
    }
}
