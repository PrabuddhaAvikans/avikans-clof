using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record TaskActionActorDto(Guid UserId, string UserName);
