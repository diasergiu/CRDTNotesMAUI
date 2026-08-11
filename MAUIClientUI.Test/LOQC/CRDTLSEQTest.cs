using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Cursor;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MAUIClientUI.Test.LOQC
{
    public class CRDTLSEQTest
    {

        private void AssertCharacterEquality(CRDTCharacterClient expected, CRDTCharacterClient actual)
        {
            Assert.Equal(expected.Character, actual.Character);
            Assert.Equal(expected.IdCharacter, actual.IdCharacter);
            Assert.Equal(expected.IdLeftCharacter, actual.IdLeftCharacter);
            Assert.Equal(expected.IdRightCharacter, actual.IdRightCharacter);
            Assert.Equal(expected.ClientId, actual.ClientId);
        }

        [Fact]
        public void TestInsertEmptyText()
        {
            NoteCursor cursor = new NoteCursor("" , Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(0, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = 0,
                IdLeftCharacter = null,
                IdRightCharacter = null,
                ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE")
            };

            AssertCharacterEquality(expected, actual);

        }

        public void TestInsertEmptyWieredPositionText()
        {
            NoteCursor cursor = new NoteCursor("", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(5, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = 5, // i am not shure if this is the desired behavior
                IdLeftCharacter = null,
                IdRightCharacter = null,
                ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE")
            };

            AssertCharacterEquality(expected, actual);

        }

        public void TestInsertEmptyUnexistingPositionText()
        {
            NoteCursor cursor = new NoteCursor("", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(10, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = 10, // i am not shure if this is the desired behavior
                IdLeftCharacter = null,
                IdRightCharacter = null,
                ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE")
            };

            AssertCharacterEquality(expected, actual);

        }
        [Fact]
        public void TextConstructWordWith10()
        {
            NoteCursor cursor = new NoteCursor("adsadasdas", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));

        }

        [Fact]
        public void TestInsertEndText()
        {
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(7, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = 7, // i am not shure if this is the desired behavior
                IdLeftCharacter = 6,
                IdRightCharacter = null,
                ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE")
            };

            AssertCharacterEquality(expected, actual);
        }
        [Fact]
        public void TestInsertFrontText()
        {
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(0, 'H');
            CRDTCharacterClient expected = new CRDTCharacterClient()
            {
                Character = 'H',
                IdCharacter = 0.5m, // i am not shure if this is the desired behavior
                IdLeftCharacter = null,
                IdRightCharacter = 1,
                ClientId = Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE")
            };

            AssertCharacterEquality(expected, actual);
        }
        [Fact]
        public void TestInsert10OnEmptyTextText()
        {

        }
        [Fact]
        public void TestInsert10inBetweenText()
        {

        }

        [Fact]
        public void saveTextToDatabaseCRDTCharacter()
        {

        }

        [Fact]
        public void InsertCharacterBetweenTowCharacterDifferentDepths()
        {
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(3, 'H');
            CRDTCharacterClient second = cursor.InsertCharacter(3, 'e');
            CRDTCharacterClient third = cursor.InsertCharacter(3, 'a');
            CRDTCharacterClient fourth = cursor.InsertCharacter(3, 'V');
            CRDTCharacterClient fifth = cursor.InsertCharacter(3, 'T');
            printCharacterList(cursor);
           
        }


        [Fact]
        public void InsertCharacterBetweenTowCharacterDifferentLocations()
        {
            NoteCursor cursor = new NoteCursor("Welcome", Guid.Parse("E33A3ADE-11DC-4B23-8A8B-8DD8D6F886FE"));
            CRDTCharacterClient actual = cursor.InsertCharacter(3, 'H');
            CRDTCharacterClient second = cursor.InsertCharacter(4, 'e');
            CRDTCharacterClient third = cursor.InsertCharacter(4, 'a');
            CRDTCharacterClient fourth = cursor.InsertCharacter(5, 'V');
            CRDTCharacterClient fifth = cursor.InsertCharacter(6, 'T');
            printCharacterList(cursor);

        }


        private void printCharacterList(NoteCursor cursor)
        {
            foreach (var character in cursor.characterList.Values)
            {
                Console.WriteLine($"Character: {character.Character}, Id: {character.IdCharacter}, LeftId: {character.IdLeftCharacter}, RightId: {character.IdRightCharacter}");
            }
        }
    }
}
