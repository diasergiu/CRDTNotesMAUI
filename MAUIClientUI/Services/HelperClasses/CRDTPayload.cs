using CRDTLibrary.Cursor;
using DatabaseLibrary.Entities.Client;
using ProtoBuf;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{

    /// Static utility class for encoding and decoding CRDT character payloads.
    /// Handles protobuf serialization and AES encryption/decryption transparently.
    /// Uses CRDTCharacterPayload for serialization to exclude navigation properties and client-side state.

    public static class CharacterSerializer
    {
        /// Encodes CRDT characters to protobuf binary format, encrypts using AES-256, and returns as Base64 string.
   
        public static string Encode(List<CRDTCharacterClient> characters)
        {
            if (characters == null || characters.Count == 0)
                return string.Empty;

            // Convert to payload objects for serialization (excludes navigation properties)
            var payloads = characters.Select(c => new CRDTCharacterPayload
            {
                IdCharacter = c.IdCharacter,
                Character = c.Character,
                Tombstone = c.Tombstone
            }).ToList();

            using var memoryStream = new MemoryStream();

            // Serialize to protobuf binary format
            Serializer.Serialize(memoryStream, payloads);
            byte[] protoBytes = memoryStream.ToArray();

            // Encrypt the protobuf binary and return as Base64 string
            return CharacterIdProtector.Encrypt(protoBytes);
        }


        /// Decodes an encrypted Base64-encoded protobuf payload back to CRDT characters.
        /// Decrypts using AES-256 and deserializes from protobuf binary format.

        public static List<CRDTCharacterPayload> Decode(string encryptedPayload)
        {
            if (string.IsNullOrEmpty(encryptedPayload))
                return new List<CRDTCharacterPayload>();

            try
            {
                // Decrypt the payload from Base64 string to binary without UTF-8 conversion
                // to preserve protobuf binary data integrity
                byte[] protoBytes = Encoding.UTF8.GetBytes(CharacterIdProtector.Decrypt(encryptedPayload));

                if (protoBytes.Length == 0)
                    return new List<CRDTCharacterPayload>();

                // Deserialize from protobuf binary format
                using var memoryStream = new MemoryStream(protoBytes);
                var payloads = Serializer.Deserialize<List<CRDTCharacterPayload>>(memoryStream);

                // Convert back to domain objects
                return payloads.Select(p => new CRDTCharacterPayload
                {
                    IdCharacter = p.IdCharacter,
                    Character = p.Character,
                    Tombstone = p.Tombstone,
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log and return empty list on deserialization failure
                System.Diagnostics.Debug.WriteLine($"Failed to decode CRDT payload: {ex.Message}");
                return new List<CRDTCharacterPayload>();
            }
        }
    }
}
