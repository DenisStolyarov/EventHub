using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Abstractions.Identity;

namespace EventHub.Infrastructure.Identity;

public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string passwordHash)
    {
        string hash = Hash(password);

        return string.Equals(hash, passwordHash, StringComparison.Ordinal);
    }
}
