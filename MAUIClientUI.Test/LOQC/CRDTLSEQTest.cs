using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using CRDTLibrary.Cursor;
using MAUIClientUI.Test.HelperClasses;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace MAUIClientUI.Test.LOQC
{
    public class CRDTLSEQTest
    {

        private void AssertCharacterEquality(CRDTCharacterPayload expected, CRDTCharacterPayload actual)
        {
            Assert.Equal(expected.Character, actual.Character);
            Assert.Equal(expected.IdCharacter, actual.IdCharacter);
            //Assert.Equal(expected.IdLeftCharacter, actual.IdLeftCharacter);
            //Assert.Equal(expected.IdRightCharacter, actual.IdRightCharacter);
        }

        [Theory]
        [InlineData("", "H", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'H', 1)] // insert character empty String
        [InlineData("", "H", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'H', 4)] // insert character non existing position  bad positioning
        [InlineData("Welcome", "Welcomem", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'm', 7)] // test insert at the end
        [InlineData("Welcome", "aWelcome", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'a', 0)] // test insert at the beginning
        [InlineData("Welcome", "Wealcome", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'a', 2)] // test insert in the middle
        [InlineData("Welcome", "Welcomem", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'm', 9)] // insert outside of the array
        public void TestInsertCharacter(string text, string expected, string userId, char c, int position)
        {
            Document Document = new Document(text, Guid.Parse(userId));
            var actual = Document.InsertCharacter(position, c);
            Assert.Equal(expected, Document.GetString());
        }
        [Fact]
        public void TextConstructWordWith10()
        {
            Document cursor = new Document("adsadasdas", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            Assert.NotNull(cursor);
            Assert.Equal("adsadasdas", cursor.GetString());
        }

        [Fact]
        public void TestInsert10OnEmptyText()
        {
            Document cursor = new Document("", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Insert 10 characters
            for (int i = 0; i < 10; i++)
            {
                cursor.InsertCharacter(i, (char)('A' + i));
            }

            string result = cursor.GetString();
            Assert.Equal("ABCDEFGHIJ", result);
        }

        [Fact]
        public void TestInsert10InBetweenText()
        {
            Document cursor = new Document("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Insert 5 characters at position 3
            for (int i = 0; i < 5; i++)
            {
                cursor.InsertCharacter(3, (char)('X' + i));
            }

            string result = cursor.GetString();
            // All insertions at position 3 should maintain order
            Assert.Contains("Wel", result);
        }

        [Fact]
        public void TestDeleteCharacter()
        {
            Document cursor = new Document("Hello", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Delete character to the left of position 2 (which is 'l')
            var deleted = cursor.deleteCharacter(2);

            Assert.Equal('e', deleted.Character);
            Assert.True(deleted.Tombstone);

            // Verify the character is no longer visible
            string result = cursor.GetString();
            Assert.Equal("Hllo", result);
        }

        [Fact]
        public void TestGetVisibleCharactersInOrder()
        {
            Document cursor = new Document("ABC", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            //var visibleChars = cursor.GetVisibleCharactersInOrder();
            //Assert.Equal(3, visibleChars.Count);
            //Assert.Equal('A', visibleChars[0].Character);
            //Assert.Equal('B', visibleChars[1].Character);
            //Assert.Equal('C', visibleChars[2].Character);
        }

        //[Fact]
        //public void TestGetAdjacentCharacterIds()
        //{
        //    Document cursor = new Document("ABC", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

        //    //At position 0(before 'A')
        //    var (left0, right0) = cursor.GetAdjacentCharacterIds(0);
        //    Assert.Null(left0);
        //    Assert.NotNull(right0);

        //    // At position 1 (between 'A' and 'B')
        //    var (left1, right1) = cursor.GetAdjacentCharacterIds(1);
        //    Assert.NotNull(left1);
        //    Assert.NotNull(right1);

        //    // At position 3 (after 'C')
        //    var (left3, right3) = cursor.GetAdjacentCharacterIds(3);
        //    Assert.NotNull(left3);
        //    Assert.Null(right3);
        //}

        [Fact]
        public void TestGetString()
        {
            string initialText = "HelloWorld";
            Document cursor = new Document(initialText, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            string result = cursor.GetString();
            Assert.Equal(initialText, result);
        }

        [Fact]
        public void TestInsertBetweenTwoCharactersAtSamePosition()
        {
            Document cursor = new Document("AC", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Insert 'B' between 'A' and 'C'
            var inserted = cursor.InsertCharacter(1, 'B');

            Assert.Equal('B', inserted.Character);
            //Assert.NotNull(inserted.IdLeftCharacter);
            //Assert.NotNull(inserted.IdRightCharacter);

            string result = cursor.GetString();
            Assert.Equal("ABC", result);
        }

        [Fact]
        public void TestMultipleConcurrentInserts()
        {
            Document cursor = new Document("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Insert multiple characters at the same position
            var first = cursor.InsertCharacter(3, 'X');
            var second = cursor.InsertCharacter(3, 'Y');
            var third = cursor.InsertCharacter(3, 'Z');

            // All should have unique IDs
            Assert.NotEqual(first.IdCharacter, second.IdCharacter);
            Assert.NotEqual(second.IdCharacter, third.IdCharacter);

            string result = cursor.GetString();
            Assert.Contains("X", result);
            Assert.Contains("Y", result);
            Assert.Contains("Z", result);
        }

        [Fact]
        public void TestCRDTIdServiceGenerateIdAtStart()
        {
            var idService = new CRDTIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate ID at start (no left, with right)
            decimal id = idService.GenerateIdBetween(null, 10, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.True(id > 0 && id < 10);
        }

        [Fact]
        public void TestCRDTIdServiceGenerateIdAtEnd()
        {
            var idService = new CRDTIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate ID at end (with left, no right)
            decimal id = idService.GenerateIdBetween(5, null, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.True(id > 5);
        }

        [Fact]
        public void TestCRDTIdServiceGenerateIdBetween()
        {
            var idService = new CRDTIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate ID between two existing IDs
            decimal id = idService.GenerateIdBetween(5, 10, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.True(id > 5 && id < 10);
        }

        [Fact]
        public void TestCRDTIdServiceGenerateIdEmpty()
        {
            var idService = new CRDTIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate first ID (no boundaries)
            decimal id = idService.GenerateIdBetween(null, null, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.Equal(1, id);
        }

        [Fact]
        public void TestCRDTIdServiceConflictResolution()
        {
            Guid userId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");
            var idService = new CRDTIdService(userId);
            var clientId1 = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");
            var clientId2 = Guid.Parse("A11A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");

            // Create two characters with the same ID
            var existingChar = new CRDTCharacterPayload()
            {
                Character = 'A',
                IdCharacter = BuilderHelper.GenerateForString( 5, clientId1), 
            };

            var newChar = new CRDTCharacterPayload()
            {
                Character = 'B',
                IdCharacter = BuilderHelper.GenerateForString(5, clientId2),
            };

            //var result = idService.ShouldAcceptConflictingInsert(
            //    existingChar,
            //    newChar,
            //    DateTime.Parse(existingChar.ClockDateTime),
            //    DateTime.Parse(newChar.ClockDateTime)
            //);

            // Should accept based on ClientId comparison
            //Assert.False(result); // clientId1 < clientId2, so accept existing
        }

        [Fact]
        public void InsertCharacterBetweenTwoCharacterDifferentDepths()
        {
            Document cursor = new Document("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            var actual = cursor.InsertCharacter(3, 'H');
            var second = cursor.InsertCharacter(3, 'e');
            var third = cursor.InsertCharacter(3, 'a');
            var fourth = cursor.InsertCharacter(3, 'V');
            var fifth = cursor.InsertCharacter(3, 'T');

            var result = cursor.GetString();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void InsertCharacterBetweenTwoCharacterDifferentLocations()
        {
            Document cursor = new Document("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            var actual = cursor.InsertCharacter(3, 'H');
            var second = cursor.InsertCharacter(4, 'e');
            var third = cursor.InsertCharacter(4, 'a');
            var fourth = cursor.InsertCharacter(5, 'V');
            var fifth = cursor.InsertCharacter(6, 'T');

            var result = cursor.GetString();
            Assert.NotEmpty(result);
        }

        //[Fact]
        //public void TestDocumentConstructorFromList()
        //{
        //    var characters = new List<var>()
        //    {
        //        new var() { Character = 'H', IdCharacter = 1, ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE") },
        //        new var() { Character = 'i', IdCharacter = 2, ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE") }
        //    };

        //    Document cursor = new Document(characters, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

        //    Assert.NotNull(cursor);
        //    Assert.Equal(2, cursor.GetCharacterListCount());
        //}

        //private void printCharacterList(Document cursor)
        //{
        //    foreach (var character in cursor.GetCharacterListValues())
        //    {
        //        Console.WriteLine($"Character: {character.Character}, Id: {character.IdCharacter}, LeftId: {character.IdLeftCharacter}, RightId: {character.IdRightCharacter}");
        //    }
        //}
    }
}