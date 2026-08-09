using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Cursor
{
    public class NoteCursor
    {
        List<CRDTCharacterClient> characterList = new List<CRDTCharacterClient>();

        public NoteCursor(String initialText)
        {
            characterList = new List<CRDTCharacterClient>();
            foreach (Char c in initialText)
            {
                characterList.Add(new CRDTCharacterClient() {
                    Character = c,
                    Tombstone = false,
                    IdCharacter = characterList.Count > 0 ? characterList[characterList.Count - 1].IdCharacter + 1 : 0,
                    IdLeftCharacter = characterList.Count > 0 ? characterList[characterList.Count - 1].IdCharacter : (int?)null,
                    IdRightCharacter = null
                });
            }
        }
        /// <summary>
        /// Get the IDs of characters immediately left and right of cursor position
        /// </summary>
        public (int? leftCharacterId, int? rightCharacterId) GetAdjacentCharacterIds(
              // might change it with something else, Like a HashSet or a Dictionary
            int cursorPosition)
        {
            if (characterList == null || characterList.Count == 0)
                return (null, null);

            // Get all non-tombstone characters in order
            var orderedChars = GetVisibleCharactersInOrder(characterList);

            // Cursor position 0 = before all characters
            if (cursorPosition == 0)
                return (null, orderedChars.Count > 0 ? orderedChars[0].IdCharacter : null);

            // Cursor at end
            if (cursorPosition >= orderedChars.Count)
                return (orderedChars.Count > 0 ? orderedChars[orderedChars.Count - 1].IdCharacter : null, null);

            // Cursor between characters
            return (orderedChars[cursorPosition - 1].IdCharacter, orderedChars[cursorPosition].IdCharacter);
        }

        /// <summary>
        /// Get visible (non-tombstone) characters in inorder traversal
        /// </summary>
        public List<CRDTCharacterClient> GetVisibleCharactersInOrder(List<CRDTCharacterClient> characters)
        {
            var result = new List<CRDTCharacterClient>();
            var visited = new HashSet<int>();

            // Find root (character with no left sibling)
            var root = characters.FirstOrDefault(c => !c.IdLeftCharacter.HasValue);

            if (root != null)
                TraverseInorder(root, result, visited, characters);

            return result.Where(c => !c.Tombstone).ToList();
        }

        private void TraverseInorder(
            CRDTCharacterClient node,
            List<CRDTCharacterClient> result,
            HashSet<int> visited,
            List<CRDTCharacterClient> allChars)
        {
            if (node == null || visited.Contains(node.IdCharacter))
                return;

            visited.Add(node.IdCharacter);

            // Left subtree
            if (node.IdLeftCharacter.HasValue)
            {
                var left = allChars.FirstOrDefault(c => c.IdCharacter == node.IdLeftCharacter);
                if (left != null)
                    TraverseInorder(left, result, visited, allChars);
            }

            // Current node
            result.Add(node);

            // Right subtree
            if (node.IdRightCharacter.HasValue)
            {
                var right = allChars.FirstOrDefault(c => c.IdCharacter == node.IdRightCharacter);
                if (right != null)
                    TraverseInorder(right, result, visited, allChars);
            }
        }

        /// <summary>
        /// Given a character position in plain text, find its CRDTCharacter ID
        /// </summary>
        public int? FindCharacterIdAtPosition(
            List<CRDTCharacterClient> characters,
            int position)
        {
            var ordered = GetVisibleCharactersInOrder(characters);

            if (position < 0 || position >= ordered.Count)
                return null;

            return ordered[position].IdCharacter;
        }

        /// <summary>
        /// Reconstruct plain text from CRDT characters
        /// </summary>
        public string ReconstructText(List<CRDTCharacterClient> characters)
        {
            var ordered = GetVisibleCharactersInOrder(characters);
            return string.Concat(ordered.Select(c => c.Character));
        }
    }
}

