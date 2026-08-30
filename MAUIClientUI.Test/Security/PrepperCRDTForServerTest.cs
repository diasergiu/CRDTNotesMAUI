using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Services.HelperClasses;
using Xunit;
using System.Collections.Generic;

namespace MAUIClientUI.Test.Security
{
    /// <summary>
    /// Unit tests for CharacterSerializer encoding and decoding.
    /// Verifies protobuf serialization and encryption of CRDT character data.
    /// </summary>
    public class PrepperCRDTForServerTest
    {
        [Fact]
        public void CharacterSerializer_Encode_WithValidCharacterList_ReturnsEncryptedString()
        {
            // Arrange
            var characters = new List<CRDTCharacterClient>
            {
                new CRDTCharacterClient { IdCharacter = "(1),(a)", Character = 'H', Tombstone = false, IsDirtyFlag = false },
                new CRDTCharacterClient { IdCharacter = "(2),(a)", Character = 'i', Tombstone = false, IsDirtyFlag = false }
            };

            // Act
            var result = CharacterSerializer.Encode(characters);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void CharacterSerializer_Encode_WithEmptyList_ReturnsEmptyString()
        {
            // Arrange
            var characters = new List<CRDTCharacterClient>();

            // Act
            var result = CharacterSerializer.Encode(characters);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void CharacterSerializer_Encode_WithNull_ReturnsEmptyString()
        {
            // Act
            var result = CharacterSerializer.Encode(null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void CharacterSerializer_Encode_WithSingleCharacter_ReturnsEncryptedString()
        {
            // Arrange
            var characters = new List<CRDTCharacterClient>
            {
                new CRDTCharacterClient { IdCharacter = "(1),(a)", Character = 'A', Tombstone = false, IsDirtyFlag = false }
            };

            // Act
            var result = CharacterSerializer.Encode(characters);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void CharacterSerializer_Encode_WithTombstonedCharacter_ReturnsEncryptedString()
        {
            // Arrange
            var characters = new List<CRDTCharacterClient>
            {
                new CRDTCharacterClient { IdCharacter = "(1),(a)", Character = 'X', Tombstone = true, IsDirtyFlag = false }
            };

            // Act
            var result = CharacterSerializer.Encode(characters);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void CharacterSerializer_Encode_WithValidData_ReturnsConsistentEncryption()
        {
            // Arrange
            var characters1 = new List<CRDTCharacterClient>
            {
                new CRDTCharacterClient { IdCharacter = "(1),(a)", Character = 'A', Tombstone = false, IsDirtyFlag = true }
            };
            var characters2 = new List<CRDTCharacterClient>
            {
                new CRDTCharacterClient { IdCharacter = "(1),(a)", Character = 'A', Tombstone = false, IsDirtyFlag = true }
            };

            // Act
            var result1 = CharacterSerializer.Encode(characters1);
            var result2 = CharacterSerializer.Encode(characters2);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void CharacterSerializer_Encode_Decode_RoundTrip_ReturnsOriginalData()
        {
            // Arrange
            var originalCharacters = new List<CRDTCharacterClient>
            {
                new CRDTCharacterClient { IdCharacter = "(1),(a)", Character = 'H', Tombstone = false, IsDirtyFlag = false },
                new CRDTCharacterClient { IdCharacter = "(2),(a)", Character = 'i', Tombstone = false, IsDirtyFlag = false }
            };

            // Act
            var encoded = CharacterSerializer.Encode(originalCharacters);
            var decoded = CharacterSerializer.Decode(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal(originalCharacters.Count, decoded.Count);
            for (int i = 0; i < originalCharacters.Count; i++)
            {
                Assert.Equal(originalCharacters[i].IdCharacter, decoded[i].IdCharacter);
                Assert.Equal(originalCharacters[i].Character, decoded[i].Character);
                Assert.Equal(originalCharacters[i].Tombstone, decoded[i].Tombstone);
            }
        }

        [Fact]
        public void CharacterSerializer_Decode_WithEmptyString_ReturnsEmptyList()
        {
            // Act
            var result = CharacterSerializer.Decode(string.Empty);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void CharacterSerializer_Decode_WithNull_ReturnsEmptyList()
        {
            // Act
            var result = CharacterSerializer.Decode(null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void CharacterSerializer_Decode_WithInvalidData_ReturnsEmptyList()
        {
            // Arrange - random Base64 string that won't decrypt properly
            var invalidPayload = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("invalid data"));

            // Act
            var result = CharacterSerializer.Decode(invalidPayload);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
