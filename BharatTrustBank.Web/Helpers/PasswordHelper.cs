using System;
using System.Security.Cryptography;
using System.Text;

namespace BharatTrustBank.Web.Helpers
{
    public static class PasswordHelper
    {
        private const int Iterations = 310_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA512, HashSize);
            return $"PBKDF2-SHA512${Iterations}${Convert.ToBase64String(hashBytes)}";
        }

        public static bool VerifyPassword(string inputPassword, string storedHash, string salt)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash)) return false;
            if (!storedHash.StartsWith("PBKDF2-SHA512$", StringComparison.Ordinal))
            {
                // Existing live accounts are upgraded after their next successful login.
                using var sha256 = SHA256.Create();
                byte[] legacyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputPassword + salt));
                return CryptographicOperations.FixedTimeEquals(legacyHash, Convert.FromBase64String(storedHash));
            }

            string[] parts = storedHash.Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int iterations)) return false;
            byte[] expected = Convert.FromBase64String(parts[2]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(inputPassword, Convert.FromBase64String(salt), iterations, HashAlgorithmName.SHA512, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        public static bool NeedsRehash(string storedHash) => !storedHash.StartsWith("PBKDF2-SHA512$", StringComparison.Ordinal);

        public static string GenerateSalt(int size = SaltSize)
        {
            byte[] saltBytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
    }
}

