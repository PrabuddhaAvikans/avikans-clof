using System.Text.Json;
namespace Sales.Application.Costing;

public sealed record CostingCommentDto(
    Guid Id,
    string Comment,
    string UserName,
    DateTimeOffset Timestamp);
