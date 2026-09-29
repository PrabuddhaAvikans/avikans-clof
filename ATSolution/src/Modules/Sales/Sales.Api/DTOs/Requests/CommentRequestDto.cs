namespace Sales.Api.DTOs.Requests;

public sealed record CommentRequestDto(string? Comment = null, string? ActorName = null);
