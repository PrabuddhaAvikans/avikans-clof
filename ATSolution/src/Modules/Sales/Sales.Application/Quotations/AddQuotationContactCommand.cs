using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Quotations;

public sealed record AddQuotationContactCommand(
    Guid QuotationId,
    string Type,
    string Summary,
    string? Detail,
    string? Outcome,
    string? ContactedBy,
    string? ContactedByName);
