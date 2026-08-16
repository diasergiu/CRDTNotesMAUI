using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using Microsoft.Maui.Layouts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MAUIClientUI.Cursor
{
    public class NoteCursor
    {
        private SortedDictionary<CRDTId, CRDTCharacterClient> _characterList;
        private SortedList<CRDTId, CRDTCharacterClient> _sortedList;
        private readonly LseqIdService _idService;
        private readonly Guid _clientId;

        public NoteCursor(String initialText, Guid clientId)
        {
            _clientId = clientId;
            _idService = new LseqIdService(clientId);
            _characterList = new SortedDictionary<CRDTId, CRDTCharacterClient>();
            _sortedList = new SortedList<CRDTId, CRDTCharacterClient>();
            int i = 0;
            foreach (Char c in initialText)
            {
                InsertCharacter(i, c);
                ++i;
            }
        }

        public NoteCursor(List<CRDTCharacterClient> listFromDataBase, Guid clientId)
        {

            _idService = new LseqIdService(clientId);
            _sortedList = new SortedList<CRDTId, CRDTCharacterClient>();
            _characterList = new SortedDictionary<CRDTId, CRDTCharacterClient>();
            if (listFromDataBase != null)
            {
                foreach (CRDTCharacterClient character in listFromDataBase)
                {

                    _characterList.Add(character.crdtId(), character);
                    _sortedList.Add(character.crdtId(), character);
                }
            }
        }

        public CRDTCharacterClient deleteCharacterToTheLeft(int cursorPosition)
        {
            var (leftId, rightId) = GetAdjacentCharacterIds(cursorPosition);
            _characterList[leftId].Tombstone = true; // dosent delete properly
            _characterList[leftId].IsDirtyFlag = true;
            return _characterList[leftId];
        }


        /// <summary>
        /// Insert character at cursor position with conflict resolution
        /// </summary>
        public CRDTCharacterClient InsertCharacter(int atPosition, char character)
        {
            var (leftId, rightId) = GetAdjacentCharacterIds(atPosition);

            // Generate unique ID using LSEQ with conflict resolution
            decimal newId = _idService.GenerateIdBetween(
                    leftId != null ? leftId.Position : null,
                    rightId != null ? rightId.Position : null,
                    _clientId);
            // if we insert a character in an Id belonginng to a deleted character
            CRDTId id = new CRDTId { Position = newId, ClientId = _clientId };
            if (_characterList.ContainsKey(id))
            {
                Debug.WriteLine($"Conflict detected for ID {newId}. Existing character: {_characterList[id].Character}, New character: {character}");
                // _characterList[newId].Tombstone = false;
                // _characterList[newId].Character = character;
                throw new Exception("tried to insert a character that already exists"); 
            }
            else
            {
                var newCharacter = new CRDTCharacterClient()
                {
                    Character = character,
                    IdCharacter = newId,
                    Tombstone = false,
                    ClientId = _clientId,
                    ClockDateTime = DateTime.UtcNow.ToString("O"),
                    Operation = "insert",
                    IsDirtyFlag = true
                };
                _characterList.Add(id, newCharacter);
                _sortedList.Add(id, newCharacter);
                return _characterList[id];
            }
            
        }
        /// Get the IDs of characters immediately left and right of cursor position
        /// </summary>
        /// 
        private (CRDTId? left, CRDTId? right) GetAdjacentCharacterIds(int currentPosition)
        {
            CRDTId? rightId = _sortedList.Count > currentPosition ? _sortedList.GetKeyAtIndex(currentPosition) : (CRDTId?)null;
            CRDTId? leftId = currentPosition > 0 && _sortedList.Count > currentPosition - 1 ? _sortedList.GetKeyAtIndex(currentPosition - 1) : (CRDTId?)null;

            return (leftId, rightId);
        }

        public string GetString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<CRDTId, CRDTCharacterClient> character in _sortedList)
            {
                if (!character.Value.Tombstone)
                {
                    builder.Append(character.Value.Character);
                }
            }

            return builder.ToString();
        }

        internal void MergeCharacter(CRDTCharacterClient c)
        {
            if (!_characterList.ContainsKey(c.crdtId()))
            {
                _sortedList.Add(c.crdtId(), c);
                _characterList.Add(c.crdtId(), c);
            }
        }

       
    }
}

