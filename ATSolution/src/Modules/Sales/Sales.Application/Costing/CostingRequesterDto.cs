using System.Text.Json;
namespace Sales.Application.Costing;

public sealed record CostingRequesterDto(
    string Name,
    string Title,
    string Email,
    string AvatarInitials);
