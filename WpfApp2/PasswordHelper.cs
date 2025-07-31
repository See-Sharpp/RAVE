using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace WpfApp2
{
    internal class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;

        public static string? HashPassword(string password)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    // A password must be provided to be hashed.
                    throw new ArgumentNullException(nameof(password));
                }

                using var algorithm = new Rfc2898DeriveBytes(
                                      password,
                                      SaltSize,
                                      Iterations,
                                      HashAlgorithmName.SHA256);
                var key = algorithm.GetBytes(KeySize);
                var salt = algorithm.Salt;

                return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
            }
            catch (Exception ex)
            {
                // Log the error for debugging and return null to indicate failure.
                Trace.WriteLine($"[PasswordHelper Error] Hashing failed: {ex.Message}");
                return null;
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                {
                    return false;
                }

                var parts = hashedPassword.Split('.', 3);
                if (parts.Length != 3)
                {
                    // This indicates the hash is malformed and cannot be valid.
                    return false;
                }

                int iterations = int.Parse(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] key = Convert.FromBase64String(parts[2]);

                using var algorithm = new Rfc2898DeriveBytes(
                                      password,
                                      salt,
                                      iterations,
                                      HashAlgorithmName.SHA256);

                byte[] keyToCheck = algorithm.GetBytes(KeySize);

                return CryptographicOperations.FixedTimeEquals(keyToCheck, key);
            }
            catch (Exception ex)
            {
                // Any exception during verification (e.g., FormatException from a corrupt hash)
                // means the password is not a match. Log the error and return false.
                Trace.WriteLine($"[PasswordHelper Error] Verification failed: {ex.Message}");
                return false;
            }
        }
    }
}