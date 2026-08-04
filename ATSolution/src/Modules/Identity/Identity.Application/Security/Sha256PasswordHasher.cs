using Identity.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Security;

internal sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        return Convert.ToBase64String(hashBytes);
    }
}