using System.Text.Json;
namespace Sales.Application.Quotations;

public sealed record AttachmentDto(
    Guid Id,
    string Name,
    long Size,
    string? ContentType,
    string? Url);
