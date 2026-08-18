using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using Microsoft.Maui.Layouts;
using Microsoft.Extensions.Logging;
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
        private SortedDictionary<string, CRDTCharacterClient> _characterList;
        private SortedList<string, CRDTCharacterClient> _sortedList;
        private readonly LseqIdService _idService;
        private readonly Guid _clientId;
        private readonly ILogger<NoteCursor> _logger;

        public NoteCursor(String initialText, Guid clientId, ILogger<NoteCursor> logger = null)
        {
            _clientId = clientId;
            _logger = logger;
            _idService = new LseqIdService(clientId);
            _characterList = new SortedDictionary<string, CRDTCharacterClient>(new CompositeIdComparator(_idService));
            _sortedList = new SortedList<string, CRDTCharacterClient>(new CompositeIdComparator(_idService));
            int i = 0;
            foreach (Char c in initialText)
            {
                InsertCharacter(i, c);
                ++i;
            }
            _logger?.LogDebug($"NoteCursor initialized with {_characterList.Count} characters for client {_clientId}");
        }

        public NoteCursor(List<CRDTCharacterClient> listFromDataBase, Guid clientId, ILogger<NoteCursor> logger = null)
        {
            _clientId = clientId;
            _logger = logger;
            _idService = new LseqIdService(clientId);
            _sortedList = new SortedList<string, CRDTCharacterClient>(new CompositeIdComparator(_idService));
            _characterList = new SortedDictionary<string, CRDTCharacterClient>(new CompositeIdComparator(_idService));
            if (listFromDataBase != null)
            {
                foreach (CRDTCharacterClient character in listFromDataBase)
                {
                    _characterList.Add(character.IdCharacter, character);
                    _sortedList.Add(character.IdCharacter, character);
                }
                _logger?.LogDebug($"NoteCursor initialized from database with {listFromDataBase.Count} characters for client {_clientId}");
            }
        }

        public CRDTCharacterClient deleteCharacterToTheLeft(int cursorPosition)
        {
            var (leftId, rightId) = GetAdjacentCharacterIds(cursorPosition);
            _characterList[leftId].Tombstone = true;
            _characterList[leftId].IsDirtyFlag = true;
            _logger?.LogDebug($"Deleted character at cursor position {cursorPosition}");
            return _characterList[leftId];
        }

        /// <summary>
        /// Insert character at cursor position with conflict resolution
        /// When collision on string ID occurs with equal boundaries, generates composite ID format: (pos,site)(pos,site)...
        /// </summary>
        public CRDTCharacterClient InsertCharacter(int atPosition, char character)
        {
            var (leftId, rightId) = GetAdjacentCharacterIds(atPosition);

            // Generate new ID between boundaries using standard LSEQ
            string newIdStr = GenerateNewId(leftId, rightId);
            _logger?.LogDebug($"Generated new ID: {newIdStr} for character '{character}' at position {atPosition}");
            if (_characterList.ContainsKey(newIdStr))
            {
                    _logger?.LogError($"Composite ID collision: {newIdStr}. This is extremely unlikely and indicates a system error.");
                    throw new Exception($"Composite ID collision: {newIdStr}. This is extremely unlikely and indicates a system error.");       
            }

            var newCharacter = new CRDTCharacterClient()
            {
                Character = character,
                IdCharacter = newIdStr,
                Tombstone = false,
                ClockDateTime = DateTime.UtcNow.ToString("O"),
                Operation = "insert",
                IsDirtyFlag = true
            };

            _characterList.Add(newIdStr, newCharacter);
            _sortedList.Add(newIdStr, newCharacter);
            return _characterList[newIdStr];
        }

        /// <summary>
        /// Generate new ID between left and right boundaries using LSEQ
        /// Returns composite ID format: (position,clientId) or (pos1,client1)(pos2,client2)...
        /// </summary>
        private string GenerateNewId(string leftIdStr, string rightIdStr)
        {
            // Check if either boundary is a composite ID
            bool leftIsComposite = !string.IsNullOrEmpty(leftIdStr) && _idService.IsCompositeId(leftIdStr);
            bool rightIsComposite = !string.IsNullOrEmpty(rightIdStr) && _idService.IsCompositeId(rightIdStr);

            // If either is composite, use composite generation logic
            if (leftIsComposite || rightIsComposite)
            {
                return _idService.GenerateIdBetweenComposite(
                    leftIdStr ?? "",
                    rightIdStr ?? "",
                    _clientId);
            }

            // For simple decimal ID case - parse the boundaries
            decimal? leftDecimal = null;
            decimal? rightDecimal = null;

            if (!string.IsNullOrEmpty(leftIdStr) && decimal.TryParse(leftIdStr, out var ld))
                leftDecimal = ld;

            if (!string.IsNullOrEmpty(rightIdStr) && decimal.TryParse(rightIdStr, out var rd))
                rightDecimal = rd;

            // Generate decimal ID
            decimal newDecimal = _idService.GenerateIdBetween(leftDecimal, rightDecimal, _clientId);

            // Convert to composite ID format: (position,clientId)
            return _idService.BuildCompositeIdString(new[] { 
                new LseqIdService.IdComponent { Position = newDecimal, SiteId = _clientId } 
            });
        }

        /// <summary>
        /// Get the IDs of characters immediately left and right of cursor position
        /// </summary>
        private (string left, string right) GetAdjacentCharacterIds(int currentPosition)
        {
            string rightId = _sortedList.Count > currentPosition ? _sortedList.Keys[currentPosition] : null;
            string leftId = currentPosition > 0 && _sortedList.Count > currentPosition - 1 ? _sortedList.Keys[currentPosition - 1] : null;

            return (leftId, rightId);
        }

        public string GetString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, CRDTCharacterClient> character in _sortedList)
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
            if (!_characterList.ContainsKey(c.IdCharacter))
            {
                _sortedList.Add(c.IdCharacter, c);
                _characterList.Add(c.IdCharacter, c);
            }
        }
    }
    /// <summary>
    /// Custom comparator for composite CRDT IDs
    /// Compares IDs by their decimal position values at each nesting level
    /// Breaks ties by comparing the site/client IDs
    /// </summary>
    public class CompositeIdComparator : IComparer<string>
    {
        private readonly LseqIdService _idService;

        public CompositeIdComparator(LseqIdService idService)
        {
            _idService = idService;
        }

        public int Compare(string x, string y)
        {
            if (x == y)
                return 0;

            if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))
                return 0;

            if (string.IsNullOrEmpty(x))
                return -1; // Empty comes first

            if (string.IsNullOrEmpty(y))
                return 1; // Empty comes first

            // Parse both IDs into components
            var xComponents = _idService.ParseCompositeId(x);
            var yComponents = _idService.ParseCompositeId(y);

            //// Try simple decimal comparison if both are simple (non-composite) decimal IDs
            //if (xComponents.Count == 0 && yComponents.Count == 0)
            //{
            //    if (decimal.TryParse(x, out decimal xDecimal) && decimal.TryParse(y, out decimal yDecimal))
            //    {
            //        return xDecimal.CompareTo(yDecimal);
            //    }
            //    return string.Compare(x, y, StringComparison.Ordinal);
            //}

            //// If one is composite and one is not, treat non-composite as decimal
            //if (xComponents.Count == 0 && decimal.TryParse(x, out decimal xDec))
            //{
            //    xComponents = new List<LseqIdService.IdComponent>
            //    {
            //        new LseqIdService.IdComponent { Position = xDec, SiteId = Guid.Empty }
            //    };
            //}

            //if (yComponents.Count == 0 && decimal.TryParse(y, out decimal yDec))
            //{
            //    yComponents = new List<LseqIdService.IdComponent>
            //    {
            //        new LseqIdService.IdComponent { Position = yDec, SiteId = Guid.Empty }
            //    };
            //}

            // Compare component by component
            int minLength = Math.Min(xComponents.Count, yComponents.Count);
            for (int i = 0; i < minLength; i++)
            {
                // First, compare by position (decimal value)
                int posComparison = xComponents[i].Position.CompareTo(yComponents[i].Position);
                if (posComparison != 0)
                    return posComparison;

                //// If positions are equal, compare by site ID (Guid)
                int siteComparison = xComponents[i].SiteId.CompareTo(yComponents[i].SiteId);
                if (siteComparison != 0 && i == xComponents.Count-1 && i == yComponents.Count - 1)
                    return siteComparison;
            }

            // If all compared components are equal, shorter ID comes first
            return xComponents.Count.CompareTo(yComponents.Count);
        }
    }
}
