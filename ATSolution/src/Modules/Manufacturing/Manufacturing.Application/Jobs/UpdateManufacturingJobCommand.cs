using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Manufacturing.Application.Jobs;

public sealed record UpdateManufacturingJobCommand(
    Guid Id,
    string? Priority,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    Guid? AssignedTo,
    string? AssignedToName,
    string? Notes,
    string? Status,
    QualityInspectionDto? QualityInspection);
