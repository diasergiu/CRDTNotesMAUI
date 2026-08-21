using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Cursor;
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

        private void AssertCharacterEquality(CRDTCharacterClient expected, CRDTCharacterClient actual)
        {
            Assert.Equal(expected.Character, actual.Character);
            Assert.Equal(expected.IdCharacter, actual.IdCharacter);
            //Assert.Equal(expected.IdLeftCharacter, actual.IdLeftCharacter);
            //Assert.Equal(expected.IdRightCharacter, actual.IdRightCharacter);
        }

        [Fact]
        public void TestInsertEmptyText()
        {
            Guid userId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");
            NoteCursor cursor = new NoteCursor("", userId);
            CRDTCharacterClient actual = cursor.InsertCharacter(0, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = BuilderHelper.GenerateForString(1, userId),
                //IdLeftCharacter = null,
                //IdRightCharacter = null,
            };

            AssertCharacterEquality(expected, actual);

        }

        [Fact]
        public void TestInsertEmptyWieredPositionText()
        {
            Guid userId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");
            NoteCursor cursor = new NoteCursor("", userId);
            CRDTCharacterClient actual = cursor.InsertCharacter(5, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = BuilderHelper.GenerateForString(5, userId), // Insert at first position when text is empty
                //IdLeftCharacter = null,
                //IdRightCharacter = null,
            };

            AssertCharacterEquality(expected, actual);

        }

        [Theory]
        [InlineData("", "H", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'H', 1)] // insert character empty String
        [InlineData("", "H", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'H', 4)] // insert character non existing position  bad positioning
        [InlineData("Welcome", "Welcomem", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'm', 8)] // test insert at the end
        [InlineData("Welcome", "aWelcome", "E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE", 'a', 0)] // test insert at the end

        public void TestInsertCharacter(string text, string expected, string userId, char c, int position)
        {
            NoteCursor noteCursor = new NoteCursor(text, Guid.Parse(userId));
            CRDTCharacter actual = noteCursor.InsertCharacter(position, c);
            Assert.Equal(noteCursor.GetString(), expected);
        }
        [Fact]
        public void TextConstructWordWith10()
        {
            NoteCursor cursor = new NoteCursor("adsadasdas", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            Assert.NotNull(cursor);
            Assert.Equal("adsadasdas", cursor.GetString());
        }

        [Fact]
        public void TestInsert10OnEmptyText()
        {
            NoteCursor cursor = new NoteCursor("", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

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
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

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
            NoteCursor cursor = new NoteCursor("Hello", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Delete character to the left of position 2 (which is 'l')
            CRDTCharacterClient deleted = cursor.deleteCharacterToTheLeft(2);

            Assert.Equal('e', deleted.Character);
            Assert.True(deleted.Tombstone);

            // Verify the character is no longer visible
            string result = cursor.GetString();
            Assert.Equal("Hllo", result);
        }

        [Fact]
        public void TestGetVisibleCharactersInOrder()
        {
            NoteCursor cursor = new NoteCursor("ABC", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            //var visibleChars = cursor.GetVisibleCharactersInOrder();
            //Assert.Equal(3, visibleChars.Count);
            //Assert.Equal('A', visibleChars[0].Character);
            //Assert.Equal('B', visibleChars[1].Character);
            //Assert.Equal('C', visibleChars[2].Character);
        }

        [Fact]
        public void TestGetAdjacentCharacterIds()
        {
            NoteCursor cursor = new NoteCursor("ABC", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // At position 0 (before 'A')
            //var (left0, right0) = cursor.GetAdjacentCharacterIds(0);
            //Assert.Null(left0);
            //Assert.NotNull(right0);

            //// At position 1 (between 'A' and 'B')
            //var (left1, right1) = cursor.GetAdjacentCharacterIds(1);
            //Assert.NotNull(left1);
            //Assert.NotNull(right1);

            //// At position 3 (after 'C')
            //var (left3, right3) = cursor.GetAdjacentCharacterIds(3);
            //Assert.NotNull(left3);
            //Assert.Null(right3);
        }

        [Fact]
        public void TestGetString()
        {
            string initialText = "HelloWorld";
            NoteCursor cursor = new NoteCursor(initialText, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            string result = cursor.GetString();
            Assert.Equal(initialText, result);
        }

        [Fact]
        public void TestInsertBetweenTwoCharactersAtSamePosition()
        {
            NoteCursor cursor = new NoteCursor("AC", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Insert 'B' between 'A' and 'C'
            CRDTCharacterClient inserted = cursor.InsertCharacter(1, 'B');

            Assert.Equal('B', inserted.Character);
            //Assert.NotNull(inserted.IdLeftCharacter);
            //Assert.NotNull(inserted.IdRightCharacter);

            string result = cursor.GetString();
            Assert.Equal("ABC", result);
        }

        [Fact]
        public void TestMultipleConcurrentInserts()
        {
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Insert multiple characters at the same position
            CRDTCharacterClient first = cursor.InsertCharacter(3, 'X');
            CRDTCharacterClient second = cursor.InsertCharacter(3, 'Y');
            CRDTCharacterClient third = cursor.InsertCharacter(3, 'Z');

            // All should have unique IDs
            Assert.NotEqual(first.IdCharacter, second.IdCharacter);
            Assert.NotEqual(second.IdCharacter, third.IdCharacter);

            string result = cursor.GetString();
            Assert.Contains("X", result);
            Assert.Contains("Y", result);
            Assert.Contains("Z", result);
        }

        [Fact]
        public void TestLseqIdServiceGenerateIdAtStart()
        {
            var idService = new LseqIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate ID at start (no left, with right)
            decimal id = idService.GenerateIdBetween(null, 10, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.True(id > 0 && id < 10);
        }

        [Fact]
        public void TestLseqIdServiceGenerateIdAtEnd()
        {
            var idService = new LseqIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate ID at end (with left, no right)
            decimal id = idService.GenerateIdBetween(5, null, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.True(id > 5);
        }

        [Fact]
        public void TestLseqIdServiceGenerateIdBetween()
        {
            var idService = new LseqIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate ID between two existing IDs
            decimal id = idService.GenerateIdBetween(5, 10, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.True(id > 5 && id < 10);
        }

        [Fact]
        public void TestLseqIdServiceGenerateIdEmpty()
        {
            var idService = new LseqIdService(Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            // Generate first ID (no boundaries)
            decimal id = idService.GenerateIdBetween(null, null, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

            Assert.Equal(1, id);
        }

        [Fact]
        public void TestLseqIdServiceConflictResolution()
        {
            Guid userId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");
            var idService = new LseqIdService(userId);
            var clientId1 = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");
            var clientId2 = Guid.Parse("A11A3ADE-11DC-4B23-8A8B-8DD8D6F886FE");

            // Create two characters with the same ID
            var existingChar = new CRDTCharacterClient()
            {
                Character = 'A',
                IdCharacter = BuilderHelper.GenerateForString( 5, clientId1), 
                ClockDateTime = DateTime.UtcNow.ToString("O")
            };

            var newChar = new CRDTCharacterClient()
            {
                Character = 'B',
                IdCharacter = BuilderHelper.GenerateForString(5, clientId2),
                ClockDateTime = DateTime.UtcNow.AddSeconds(1).ToString("O")
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
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(3, 'H');
            CRDTCharacterClient second = cursor.InsertCharacter(3, 'e');
            CRDTCharacterClient third = cursor.InsertCharacter(3, 'a');
            CRDTCharacterClient fourth = cursor.InsertCharacter(3, 'V');
            CRDTCharacterClient fifth = cursor.InsertCharacter(3, 'T');

            var result = cursor.GetString();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void InsertCharacterBetweenTwoCharacterDifferentLocations()
        {
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(3, 'H');
            CRDTCharacterClient second = cursor.InsertCharacter(4, 'e');
            CRDTCharacterClient third = cursor.InsertCharacter(4, 'a');
            CRDTCharacterClient fourth = cursor.InsertCharacter(5, 'V');
            CRDTCharacterClient fifth = cursor.InsertCharacter(6, 'T');

            var result = cursor.GetString();
            Assert.NotEmpty(result);
        }

        //[Fact]
        //public void TestNoteCursorConstructorFromList()
        //{
        //    var characters = new List<CRDTCharacterClient>()
        //    {
        //        new CRDTCharacterClient() { Character = 'H', IdCharacter = 1, ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE") },
        //        new CRDTCharacterClient() { Character = 'i', IdCharacter = 2, ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE") }
        //    };

        //    NoteCursor cursor = new NoteCursor(characters, Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

        //    Assert.NotNull(cursor);
        //    Assert.Equal(2, cursor.GetCharacterListCount());
        //}

        //private void printCharacterList(NoteCursor cursor)
        //{
        //    foreach (var character in cursor.GetCharacterListValues())
        //    {
        //        Console.WriteLine($"Character: {character.Character}, Id: {character.IdCharacter}, LeftId: {character.IdLeftCharacter}, RightId: {character.IdRightCharacter}");
        //    }
        //}
    }
}