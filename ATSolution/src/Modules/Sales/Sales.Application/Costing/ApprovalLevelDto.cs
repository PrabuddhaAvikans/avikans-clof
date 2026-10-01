using System.Text.Json;
namespace Sales.Application.Costing;

public sealed record ApprovalLevelDto(
    Guid Id,
    string Role,
    string AssigneeName,
    string Status);
