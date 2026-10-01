using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Costing;

public sealed record CostingDecisionCommand(Guid Id, string? Comment, string? ActorName);
