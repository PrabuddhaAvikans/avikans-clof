using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Catalog.Application.Products;

public sealed record ReviseProductVersionCommand(
    Guid ProductId,
    Guid SourceVersionId,
    string? RevisionNotes = null);
