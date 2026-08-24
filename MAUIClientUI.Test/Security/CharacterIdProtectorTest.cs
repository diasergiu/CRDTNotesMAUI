using MAUIClientUI.Services.HelperClasses;
using Xunit;

namespace MAUIClientUI.Test.Security
{
    /// <summary>
    /// Comprehensive tests for CharacterIdProtector encryption and decryption.
    /// Verifies deterministic AES encryption, null/empty handling, and round-trip encryption/decryption.
    /// </summary>
    public class CharacterIdProtectorTest
    {
        #region Encrypt Tests

        [Fact]
        public void Encrypt_WithValidString_ReturnsCiphertext()
        {
            // Arrange
            var plainText = "test-id-12345";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(plainText, encrypted);
        }

        [Fact]
        public void Encrypt_WithNull_ReturnsNull()
        {
            // Arrange
            string plainText = null;

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.Null(encrypted);
        }

        [Fact]
        public void Encrypt_WithEmptyString_ReturnsEmpty()
        {
            // Arrange
            var plainText = string.Empty;

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.Empty(encrypted);
        }

        [Fact]
        public void Encrypt_WithWhitespace_ReturnsEncryptedWhitespace()
        {
            // Arrange
            var plainText = "   ";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(plainText, encrypted);
        }

        [Fact]
        public void Encrypt_WithSpecialCharacters_ReturnsCiphertext()
        {
            // Arrange
            var plainText = "id-with-special-!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(plainText, encrypted);
        }

        [Fact]
        public void Encrypt_WithUnicodeCharacters_ReturnsCiphertext()
        {
            // Arrange
            var plainText = "id-with-unicode-🔒🔐✨";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(plainText, encrypted);
        }

        [Fact]
        public void Encrypt_WithLongString_ReturnsCiphertext()
        {
            // Arrange
            var plainText = new string('a', 1000);

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(plainText, encrypted);
        }

        #endregion

        #region Decrypt Tests

        [Fact]
        public void Decrypt_WithNull_ReturnsNull()
        {
            // Arrange
            string cipherText = null;

            // Act
            var decrypted = CharacterIdProtector.Decrypt(cipherText);

            // Assert
            Assert.Null(decrypted);
        }

        [Fact]
        public void Decrypt_WithEmptyString_ReturnsEmpty()
        {
            // Arrange
            var cipherText = string.Empty;

            // Act
            var decrypted = CharacterIdProtector.Decrypt(cipherText);

            // Assert
            Assert.Empty(decrypted);
        }

        [Fact]
        public void Decrypt_WithInvalidBase64_ReturnsCipherTextAsIs()
        {
            // Arrange - invalid base64 string
            var cipherText = "not-valid-base64!!!";

            // Act
            var decrypted = CharacterIdProtector.Decrypt(cipherText);

            // Assert - should handle gracefully and return original (legacy plaintext fallback)
            Assert.Equal(cipherText, decrypted);
        }

        [Fact]
        public void Decrypt_WithValidBase64ButNotEncryptedText_ReturnsCipherTextAsIs()
        {
            // Arrange - valid base64 but not encrypted by our algorithm
            var cipherText = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("random-data"));

            // Act
            var decrypted = CharacterIdProtector.Decrypt(cipherText);

            // Assert - should handle gracefully and return original (legacy plaintext fallback)
            Assert.Equal(cipherText, decrypted);
        }

        #endregion

        #region Round-Trip Encryption/Decryption Tests

        [Fact]
        public void Encrypt_ThenDecrypt_WithSimpleId_ReturnsOriginal()
        {
            // Arrange
            var plainText = "simple-id";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_ThenDecrypt_WithCompositeId_ReturnsOriginal()
        {
            // Arrange - composite CRDT ID format: (pos,site)(pos,site)...
            var plainText = "(1,11111111-1111-1111-1111-111111111111)(2,22222222-2222-2222-2222-222222222222)";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_ThenDecrypt_WithSpecialCharacters_ReturnsOriginal()
        {
            // Arrange
            var plainText = "id!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_ThenDecrypt_WithUnicodeCharacters_ReturnsOriginal()
        {
            // Arrange
            var plainText = "id-🔒🔐✨-unicode";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_ThenDecrypt_WithLongString_ReturnsOriginal()
        {
            // Arrange
            var plainText = new string('x', 1000);

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_ThenDecrypt_WithEmptyString_ReturnsEmpty()
        {
            // Arrange
            var plainText = string.Empty;

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Empty(decrypted);
        }

        [Fact]
        public void Encrypt_ThenDecrypt_WithNull_ReturnsNull()
        {
            // Arrange
            string plainText = null;

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Null(decrypted);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Encrypt_SameInput_ProducesConsistentOutput()
        {
            // Arrange
            var plainText = "deterministic-id";

            // Act
            var encrypted1 = CharacterIdProtector.Encrypt(plainText);
            var encrypted2 = CharacterIdProtector.Encrypt(plainText);
            var encrypted3 = CharacterIdProtector.Encrypt(plainText);

            // Assert - same plaintext should always produce same ciphertext (deterministic encryption)
            Assert.Equal(encrypted1, encrypted2);
            Assert.Equal(encrypted2, encrypted3);
        }

        [Fact]
        public void Encrypt_DifferentInputs_ProduceDifferentOutput()
        {
            // Arrange
            var plainText1 = "id-1";
            var plainText2 = "id-2";

            // Act
            var encrypted1 = CharacterIdProtector.Encrypt(plainText1);
            var encrypted2 = CharacterIdProtector.Encrypt(plainText2);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2);
        }

        #endregion

        #region MultipleRoundTrips Tests

        [Fact]
        public void MultipleEncryptDecryptCycles_WithSameData_MaintainsConsistency()
        {
            // Arrange
            var plainText = "multi-cycle-id";
            var results = new List<string>();

            // Act
            for (int i = 0; i < 5; i++)
            {
                var encrypted = CharacterIdProtector.Encrypt(plainText);
                var decrypted = CharacterIdProtector.Decrypt(encrypted);
                results.Add(decrypted);
            }

            // Assert - all cycles should recover the same plaintext
            Assert.All(results, result => Assert.Equal(plainText, result));
        }

        [Theory]
        [InlineData("id-1")]
        [InlineData("id-with-dashes-123")]
        [InlineData("(1,guid)(2,guid)")]
        [InlineData("!@#$%^&*")]
        public void Encrypt_ThenDecrypt_WithVariousInputs_ReturnsOriginal(string plainText)
        {
            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        #endregion

        #region LegacyPlaintextFallback Tests

        [Fact]
        public void Decrypt_WithPlaintextId_HandlesGracefully()
        {
            // Arrange - a plaintext ID that wasn't encrypted by the protector
            var plaintextId = "legacy-plaintext-id";

            // Act
            var decrypted = CharacterIdProtector.Decrypt(plaintextId);

            // Assert - should return original on decryption failure (legacy support)
            Assert.Equal(plaintextId, decrypted);
        }

        [Fact]
        public void Decrypt_MixedEncryptedAndPlaintextIds_BothDecodable()
        {
            // Arrange
            var plaintextId = "plaintext-id";
            var encryptedPlaintextId = CharacterIdProtector.Encrypt("encrypted-id");

            // Act
            var decryptedPlaintext = CharacterIdProtector.Decrypt(plaintextId);
            var decryptedEncrypted = CharacterIdProtector.Decrypt(encryptedPlaintextId);

            // Assert
            Assert.Equal(plaintextId, decryptedPlaintext);
            Assert.Equal("encrypted-id", decryptedEncrypted);
        }

        #endregion

        #region CRDTSpecificTests

        [Fact]
        public void Encrypt_WithCRDTCompositeId_PreservesBijectiveMapping()
        {
            // Arrange - realistic CRDT composite ID with position and site (guid)
            var plainText = "(1,11111111-1111-1111-1111-111111111111)(2,22222222-2222-2222-2222-222222222222)(3,33333333-3333-3333-3333-333333333333)";

            // Act
            var encrypted = CharacterIdProtector.Encrypt(plainText);
            var decrypted = CharacterIdProtector.Decrypt(encrypted);

            // Assert - encryption should be bijective for same plaintext -> same ciphertext
            var reencrypted = CharacterIdProtector.Encrypt(plainText);
            Assert.Equal(encrypted, reencrypted);
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_CRDTIds_CanBeUsedAsLookupKeys()
        {
            // Arrange - simulate CRDT ID being used as dictionary key
            var originalId = "(1,11111111-1111-1111-1111-111111111111)";
            var encryptedId = CharacterIdProtector.Encrypt(originalId);
            var idDictionary = new Dictionary<string, string> { { encryptedId, "data" } };

            // Act - encrypt the same id again and try to lookup
            var lookupEncrypted = CharacterIdProtector.Encrypt(originalId);
            var found = idDictionary.TryGetValue(lookupEncrypted, out var value);

            // Assert - deterministic encryption allows same key to be looked up
            Assert.True(found);
            Assert.Equal("data", value);
        }

        #endregion
    }
}
