using System.Text.Json;
namespace Sales.Application.Costing;

public sealed record ApprovalHistoryEntryDto(
    Guid Id,
    string Action,
    string UserName,
    DateTimeOffset Timestamp,
    string? Comment);
