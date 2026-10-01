using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Manufacturing.Application.Jobs;

public sealed record CreateManufacturingJobCommand(
    Guid SalesOrderId,
    Guid ProductId,
    Guid? ProductVersionId,
    decimal Quantity,
    string Priority,
    DateTimeOffset PlannedStartDate,
    DateTimeOffset PlannedEndDate,
    Guid? AssignedTo,
    string? Notes,
    string? CreatedBy,
    string? CreatedByName);
