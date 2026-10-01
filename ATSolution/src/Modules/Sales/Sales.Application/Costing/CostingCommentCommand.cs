using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Costing;

public sealed record CostingCommentCommand(Guid Id, string Comment, string? ActorName);
