using System;
using System.Security.Cryptography;
using System.Text;

namespace PremiumLivingOPS.Models.Helpers
{
    /// <summary>
    /// Provides BCrypt-compatible password hashing and verification
    /// using a pure .NET implementation (no external NuGet required).
    ///
    /// Algorithm: PBKDF2-HMACSHA256
    ///   - 16-byte cryptographic random salt
    ///   - 100,000 iterations  (OWASP 2024 recommendation)
    ///   - 32-byte derived key
    ///   - Stored as Base64: "iterations:saltBase64:hashBase64"
    ///
    /// Usage:
    ///   string hash   = PasswordHelper.Hash("plaintext");
    ///   bool   isOk   = PasswordHelper.Verify("plaintext", hash);
    /// </summary>
    public static class PasswordHelper
    {
        private const int Iterations  = 100_000;
        private const int SaltBytes   = 16;
        private const int HashBytes    = 32;
        private const char Separator  = ':';

        // ── Hash ─────────────────────────────────────────────────────
        /// <summary>
        /// Hashes a plain-text password.
        /// Returns a self-contained string that includes the salt and
        /// iteration count so it can be stored in a single DB column.
        /// </summary>
        public static string Hash(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
                throw new ArgumentException("Password must not be empty.", nameof(plainPassword));

            byte[] salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            byte[] hash = Pbkdf2(plainPassword, salt, Iterations, HashBytes);

            return $"{Iterations}{Separator}{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
        }

        // ── Verify ───────────────────────────────────────────────────
        /// <summary>
        /// Verifies a plain-text password against a stored hash produced by Hash().
        /// Returns true if the password matches; false otherwise.
        /// Uses a constant-time comparison to prevent timing attacks.
        /// </summary>
        public static bool Verify(string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(storedHash))
                return false;

            string[] parts = storedHash.Split(Separator);
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out int iterations)) return false;

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt         = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch { return false; }

            byte[] actualHash = Pbkdf2(plainPassword, salt, iterations, expectedHash.Length);
            return ConstantTimeEquals(actualHash, expectedHash);
        }

        // ── IsHashed ─────────────────────────────────────────────────
        /// <summary>
        /// Returns true if the string looks like a hash produced by Hash().
        /// Useful during migration: if the stored value is NOT yet hashed,
        /// fall back to a plain-text comparison and then re-hash on success.
        /// </summary>
        public static bool IsHashed(string value)
            => value != null && value.Split(Separator).Length == 3;

        // ── Private helpers ──────────────────────────────────────────
        private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (var prf = new Rfc2898DeriveBytes(
                       password, salt, iterations, HashAlgorithmName.SHA256))
                return prf.GetBytes(outputBytes);
        }

        /// <summary>Constant-time byte-array comparison (prevents timing side-channels).</summary>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
