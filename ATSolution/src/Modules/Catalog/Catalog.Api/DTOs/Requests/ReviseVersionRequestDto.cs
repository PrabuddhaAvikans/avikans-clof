namespace Catalog.Api.DTOs.Requests;

public sealed record ReviseVersionRequestDto
{
    public string? RevisionNotes { get; init; }
}
