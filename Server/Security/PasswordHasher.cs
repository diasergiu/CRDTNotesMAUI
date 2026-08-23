using System;
using System.Security.Cryptography;

namespace Server.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
        private const char Delimiter = '.';

        public static string Hash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return string.Join(
                Delimiter,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        public static bool Verify(string password, string hashedPassword)
        {
            if (password == null || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            string[] segments = hashedPassword.Split(Delimiter);
            if (segments.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(segments[0], out int iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] hash;
            try
            {
                salt = Convert.FromBase64String(segments[1]);
                hash = Convert.FromBase64String(segments[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, hash.Length);
            return CryptographicOperations.FixedTimeEquals(inputHash, hash);
        }
    }
}
