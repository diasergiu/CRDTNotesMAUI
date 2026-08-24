using System;
using System.Security.Cryptography;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    /// <summary>
    /// Deterministic AES encryption for CRDTCharacter.IdCharacter values.
    /// The client encrypts ids before sending changes to the server and decrypts
    /// them when receiving changes, so the server only ever stores ciphertext.
    /// Determinism (fixed key + IV) is required because IdCharacter is part of the
    /// composite primary key: the same logical id must always produce the same
    /// ciphertext so updates and lookups keep matching existing rows.
    /// </summary>
    public static class CharacterIdProtector
    {
        // NOTE: development-only static key/IV. In production these should come
        // from a secure configuration/secret store.
        // Key: 32 bytes (256-bit key for AES-256)
        // IV: Must be exactly 128-bit (16 bytes) for AES
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("crdt-notes-idchar-key-32byteslen");   // 32 bytes
        private static readonly byte[] Iv = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 }; // Exactly 16 bytes

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] input = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher = encryptor.TransformFinalBlock(input, 0, input.Length);
            return Convert.ToBase64String(cipher);
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            try
            {
                byte[] cipher = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
            {
                // Value was not encrypted (e.g. legacy plaintext) - return as-is.
                return cipherText;
            }
        }
    }
}
