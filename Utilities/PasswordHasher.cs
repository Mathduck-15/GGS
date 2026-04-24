using System.Security.Cryptography;
using System.Text;

namespace GoodGovernanceApp.Utilities;

/// <summary>
/// Handles password hashing and verification.
///
/// Storage format (new passwords): SHA-256 hex, 64 lowercase chars.
///
/// Legacy support:
///   • bcrypt hashes  ($2a/$2b/$2y) — produced by Laravel / PHP seeders.
///   • SHA-256 hashes (any casing)  — produced by this or older versions of this app.
/// </summary>
public static class PasswordHasher
{
    // ── Hashing ────────────────────────────────────────────────────────────

    /// <summary>Returns the lowercase hex SHA-256 digest of the plain-text password.</summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder(64);
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));   // always lowercase
        return sb.ToString();
    }

    // ── Verification ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies <paramref name="plainText"/> against any stored hash format.
    ///
    /// Detection order:
    ///   1. BCrypt  — stored hash starts with $2a$, $2b$, or $2y$.
    ///   2. SHA-256 — 64-char hex string (case-insensitive, whitespace-trimmed).
    ///   3. Plain   — direct string match (fallback for un-hashed legacy rows).
    /// </summary>
    public static bool VerifyPassword(string plainText, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        // Normalise: strip any invisible characters / whitespace the DB may add
        storedHash = storedHash.Trim();

        // ── 1. BCrypt (Laravel / PHP bcrypt hashes) ───────────────────────
        if (storedHash.StartsWith("$2a$") ||
            storedHash.StartsWith("$2b$") ||
            storedHash.StartsWith("$2y$"))
        {
            try { return BCrypt.Net.BCrypt.Verify(plainText, storedHash); }
            catch { return false; }
        }

        // ── 2. SHA-256 hex (this application's standard format) ───────────
        if (storedHash.Length == 64 && IsHex(storedHash))
        {
            string inputHash = HashPassword(plainText);

            // Case-insensitive comparison covers databases that store uppercase
            return string.Equals(inputHash, storedHash,
                                 StringComparison.OrdinalIgnoreCase);
        }

        // ── 3. Plain-text fallback (should never happen in production) ────
        return string.Equals(plainText, storedHash, StringComparison.Ordinal);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static bool IsHex(string s)
    {
        foreach (char c in s)
            if (!((c >= '0' && c <= '9') ||
                  (c >= 'a' && c <= 'f') ||
                  (c >= 'A' && c <= 'F')))
                return false;
        return true;
    }
}
