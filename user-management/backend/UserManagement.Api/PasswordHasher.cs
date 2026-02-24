using System.Security.Cryptography;

namespace UserManagement.Api;

public sealed class PasswordHasher
{
    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string expectedHash, string expectedSalt)
    {
        var salt = Convert.FromBase64String(expectedSalt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(expectedHash));
    }
}
