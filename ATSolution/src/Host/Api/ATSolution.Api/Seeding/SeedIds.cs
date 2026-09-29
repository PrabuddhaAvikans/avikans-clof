using System.Security.Cryptography;
using System.Text;

namespace ATSolution.Api.Seeding;

/// <summary>
/// Deterministic Guid mapping for mock string IDs (e.g. cat-001 → stable Guid).
/// </summary>
public static class SeedIds
{
    private const string Prefix = "ats:";

    public static Guid ToGuid(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(Prefix + id.Trim()));
        return new Guid(bytes);
    }

    public static Guid? ToGuidOrNull(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : ToGuid(id);
}
