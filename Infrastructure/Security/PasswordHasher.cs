using Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public byte[] Hash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        return sha256.ComputeHash(bytes);
    }

    public bool Verify(string password, byte[] storedHash)
    {
        var candidateHash = Hash(password);
        return CryptographicOperations.FixedTimeEquals(candidateHash, storedHash);
    }
}
