namespace ATSolution.Application.Abstractions.Identity;

/// <summary>
/// Request-scoped actor context for audit and authorization helpers.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string UserName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
