using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Costing;

public sealed record UpdateCostingNotesCommand(Guid Id, string Notes);
