using System.Security.Cryptography;
using System.Text;

namespace GoodGovernanceApp.Utilities;

/// <summary>
/// Provides SHA-256 password hashing.
///
/// IMPORTANT — Seeded passwords via Laravel's `bcrypt()` use a completely
/// different algorithm (bcrypt/Argon2). If your seeder used bcrypt, the stored
/// hash will never match a SHA-256 hash. In that case you have two options:
///   A) Re-seed the users table with SHA-256 hashes (call HashPassword here).
///   B) Add BCrypt.Net-Next NuGet package and verify with BCrypt.Verify().
///
/// This app currently uses SHA-256. Keep it consistent in the seeder.
/// </summary>
public static class PasswordHasher
{
    /// <summary>Returns the lowercase hex SHA-256 digest of the given plain-text password.</summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder(64);
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>Constant-time comparison to prevent timing attacks.</summary>
    public static bool VerifyPassword(string plainText, string storedHash)
    {
        string inputHash = HashPassword(plainText);
        // CryptographicOperations.FixedTimeEquals prevents timing side-channels.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(inputHash),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
