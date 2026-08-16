using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using Microsoft.Maui.Layouts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MAUIClientUI.Cursor
{
    public class NoteCursor
    {
        private SortedDictionary<decimal, CRDTCharacter> _characterList;
        private SortedList<decimal, CRDTCharacter> _sortedList;
        private readonly LseqIdService _idService;
        private readonly Guid _clientId;

        public NoteCursor(String initialText, Guid clientId)
        {
            _clientId = clientId;
            _idService = new LseqIdService(clientId);
            _characterList = new SortedDictionary<decimal, CRDTCharacter>();
            _sortedList = new SortedList<decimal, CRDTCharacter>();
            int i = 0;
            foreach (Char c in initialText)
            {
                InsertCharacter(i, c);
                ++i;
            }
        }

        public NoteCursor(List<CRDTCharacter> listFromDataBase, Guid clientId)
        {
           
            _idService = new LseqIdService(clientId);
            _sortedList = new SortedList<decimal, CRDTCharacter>();
            _characterList = new SortedDictionary<decimal, CRDTCharacter>();
            if (listFromDataBase != null)
            {
                foreach (CRDTCharacter character in listFromDataBase)
                {
                    
                    _characterList.Add(character.IdCharacter, character);
                    _sortedList.Add(character.IdCharacter, character);
                }
            }
        }

        public CRDTCharacter deleteCharacterToTheLeft(int cursorPosition)
        {
            var (leftId, rightId) = GetAdjacentCharacterIds(cursorPosition);
            _characterList[(decimal)leftId].Tombstone = true; // dosent delete properly
            _characterList[(decimal)leftId].IsDirtyFlag = true;
            return _characterList[(decimal)leftId];
        }


        /// <summary>
        /// Insert character at cursor position with conflict resolution
        /// </summary>
        public CRDTCharacter InsertCharacter(int atPosition, char character)
        {
            var (leftId, rightId) = GetAdjacentCharacterIds(atPosition);

            // Generate unique ID using LSEQ with conflict resolution
            decimal newId = _idService.GenerateIdBetween(leftId, rightId, _clientId);
            // if we insert a character in an Id belonginng to a deleted character
            if (_characterList.ContainsKey(newId))
            {
                _characterList[newId].Tombstone = false;
                _characterList[newId].Character = character;
            }
            else
            {
                var newCharacter = new CRDTCharacter()
                {
                    Character = character,
                    IdCharacter = newId,
                    Tombstone = false,
                    ClientId = _clientId,
                    ClockDateTime = DateTime.UtcNow.ToString("O"),
                    Operation = "insert",
                    IsDirtyFlag = true
                };

                _characterList.Add(newId, newCharacter);
                _sortedList.Add(newId, newCharacter);
                //if (leftId.HasValue && characterList.ContainsKey(leftId.Value))
                //{
                //    characterList[leftId.Value].IdRightCharacter = newCharacter.IdCharacter;
                //}
                //if (rightId.HasValue && characterList.ContainsKey(rightId.Value))
                //{
                //    characterList[rightId.Value].IdLeftCharacter = newCharacter.IdCharacter;
                //}
            }
            return _characterList[newId];
        }

        /// <summary>
        /// Handle concurrent inserts that generated the same ID
        /// </summary>
        //public void ResolveConflictingInsert(
        //    CRDTCharacter existingChar,
        //    CRDTCharacter incomingChar)
        //{
        //    var existingTime = DateTime.Parse(existingChar.ClockDateTime);
        //    var incomingTime = DateTime.Parse(incomingChar.ClockDateTime);

        //    bool acceptIncoming = _idService.ShouldAcceptConflictingInsert(
        //        existingChar,
        //        incomingChar,
        //        existingTime,
        //        incomingTime);

        //    if (acceptIncoming)
        //    {
        //        // Remove or tombstone the existing character
        //        var idx = characterList[existingChar.IdCharacter];
        //        //FindIndex(c => c.IdCharacter == existingChar.IdCharacter);
        //        if (idx >= 0)
        //            characterList[idx].Tombstone = true;

        //        // Add the incoming character
        //        characterList.Add(incomingChar);
        //    }
        //    else
        //    {
        //        // Keep existing, tombstone incoming
        //        incomingChar.Tombstone = true;
        //        characterList.Add(incomingChar);
        //    }
        //}

        ///// <summary>
        ///// Resolve batch of concurrent operations with potential ID collisions
        ///// </summary>
        //public void ResolveBatchConflicts(List<CRDTCharacter> incomingChars)
        //{
        //    var resolved = _idService.ResolveBatchConflicts(characterList, incomingChars);

        //    // Update character list with resolved state
        //    foreach (var ch in resolved)
        //    {
        //        var existing = characterList.FirstOrDefault(c => c.IdCharacter == ch.IdCharacter);
        //        if (existing != null)
        //            existing.Tombstone = ch.Tombstone;
        //        else
        //            characterList.Add(ch);
        //    }
        //}
        /// <summary>
        /// Get the IDs of characters immediately left and right of cursor position
        /// </summary>
        /// 
        private (decimal? left, decimal? right) GetAdjacentCharacterIds(int currentPosition)
        {
            decimal? rightId = _sortedList.Count > currentPosition ? _sortedList.GetKeyAtIndex(currentPosition) : (decimal?)null;
            decimal? leftId = currentPosition > 0 && _sortedList.Count > currentPosition - 1 ? _sortedList.GetKeyAtIndex(currentPosition - 1) : (decimal?)null; 
        
            return (leftId, rightId);
        }
        //public (decimal? leftCharacterId, decimal? rightCharacterId) GetAdjacentCharacterIds(
        //      // might change it with something else, Like a HashSet or a Dictionary
        //    int cursorPosition)
        //{
        //    if (characterList == null || characterList.Count == 0)
        //        return (null, null);

        //    // Get all non-tombstone characters in order
        //    var orderedChars = GetVisibleCharactersInOrder();

        //    // Cursor position 0 = before all characters
        //    if (cursorPosition == 0)
        //        return (null, orderedChars.Count > 0 ? orderedChars[0].IdCharacter : null);

        //    // Cursor at end
        //    if (cursorPosition >= orderedChars.Count)
        //        return (orderedChars.Count > 0 ? orderedChars[orderedChars.Count - 1].IdCharacter : null, null);

        //    // Cursor between characters
        //    return (orderedChars[cursorPosition - 1].IdCharacter, orderedChars[cursorPosition].IdCharacter);
        //}

        ///// <summary>
        ///// Get visible (non-tombstone) characters in inorder traversal
        ///// </summary>
        //public List<CRDTCharacter> GetVisibleCharactersInOrder()
        //{
        //    var result = new List<CRDTCharacter>();
        //    var visited = new HashSet<decimal>();

        //    // Find root (character with no left sibling)
        //    var root= characterList
        //        .FirstOrDefault(x => !x.Value.IdLeftCharacter.HasValue);

        //    if (root.Value != null)
        //        TraverseInorder(root.Value, result, visited);

        //    //return result.Where(c => !c.Tombstone).ToList();
        //    return result.ToList();
        //}

        public string GetString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<decimal, CRDTCharacter> character in _sortedList)
            {
                builder.Append(character.Value.Character);
            }

            return builder.ToString();
        }

        internal void MergeCharacter(CRDTCharacter c)
        {
            if (!_characterList.ContainsKey(c.IdCharacter))
            {
                _sortedList.Add(c.IdCharacter, c);
                _characterList.Add(c.IdCharacter, c);
            }
        }

        //private void TraverseInorder(CRDTCharacter node, List<CRDTCharacter> result, HashSet<decimal> visited)
        //{
        //    if (node == null || visited.Contains(node.IdCharacter))
        //        return;

        //    visited.Add(node.IdCharacter);

        //    // Left subtree
        //    if (node.IdLeftCharacter.HasValue)
        //    {
        //        var left = characterList.FirstOrDefault(c => c.Value.IdCharacter == node.IdLeftCharacter);
        //        if (left.Value != null)
        //            TraverseInorder(left.Value, result, visited);
        //    }

        //    // Current node
        //    result.Add(node);

        //    // Right subtree
        //    if (node.IdRightCharacter.HasValue)
        //    {
        //        var right = characterList.FirstOrDefault(c => c.Value.IdCharacter == node.IdRightCharacter);
        //        if (right.Value != null)
        //            TraverseInorder(right.Value, result, visited);
        //    }
        //}


        /// <summary>
        /// Given a character position in plain text, find its CRDTCharacter ID
        /// </summary>
        //public int? FindCharacterIdAtPosition(
        //    List<CRDTCharacter> characters,
        //    int position)
        //{
        //    var ordered = GetVisibleCharactersInOrder(characters);

        //    if (position < 0 || position >= ordered.Count)
        //        return null;

        //    return ordered[position].IdCharacter;
        //}

        ///// <summary>
        ///// Reconstruct plain text from CRDT characters
        ///// </summary>
        //public string ReconstructText(List<CRDTCharacter> characters)
        //{
        //    var ordered = GetVisibleCharactersInOrder(characters);
        //    return string.Concat(ordered.Select(c => c.Character));
        //}
    }
}

