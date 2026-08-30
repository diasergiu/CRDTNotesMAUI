using Microsoft.Extensions.Logging;
using System.Text;

namespace CRDTLibrary.Cursor
{
    public class Document
    {
        private SortedDictionary<string, CRDTCharacterPayload> _characterList;
        private SortedList<string, CRDTCharacterPayload> _sortedList;
        private readonly CRDTIdService _idService;
        private readonly Guid _clientId;
        private readonly ILogger<Document> _logger;

        public Document(String initialText, Guid clientId, ILogger<Document> logger = null)
        {
            _clientId = clientId;
            _logger = logger;
            _idService = new CRDTIdService(clientId);
            _characterList = new SortedDictionary<string, CRDTCharacterPayload>(new CompositeIdComparator(_idService));
            _sortedList = new SortedList<string, CRDTCharacterPayload>(new CompositeIdComparator(_idService));
            int i = 0;
            foreach (Char c in initialText)
            {
                InsertCharacter(i, c);
                ++i;
            }
            _logger?.LogDebug($"Document initialized with {_characterList.Count} characters for client {_clientId}");
        }

        public Document(List<CRDTCharacterPayload> listFromDataBase, Guid clientId, ILogger<Document> logger = null)
        {
            _clientId = clientId;
            _logger = logger;
            _idService = new CRDTIdService(clientId);
            _sortedList = new SortedList<string, CRDTCharacterPayload>(new CompositeIdComparator(_idService));
            _characterList = new SortedDictionary<string, CRDTCharacterPayload>(new CompositeIdComparator(_idService));
            if (listFromDataBase != null)
            {
                foreach (CRDTCharacterPayload character in listFromDataBase)
                {
                    _characterList.Add(character.IdCharacter, character);
                    _sortedList.Add(character.IdCharacter, character);
                }
                _logger?.LogDebug($"Document initialized from database with {listFromDataBase.Count} characters for client {_clientId}");
            }
        }

        public CRDTCharacterPayload deleteCharacter(int cursorPosition)
        {
           var (leftId, rightId) = GetAdjacentCharacterIds(cursorPosition);
            _characterList[leftId].Tombstone = true;
            _logger?.LogDebug($"Deleted character at cursor position {cursorPosition}");
            return _characterList[leftId];
        }

        /// <summary>
        /// Insert character at cursor position with conflict resolution
        /// When collision on string ID occurs with equal boundaries, generates composite ID format: (pos,site)(pos,site)...
        /// </summary>
        public CRDTCharacterPayload InsertCharacter(int atPosition, char character)
        {
            if(atPosition > _sortedList.Count)
            {
                atPosition = _sortedList.Count;
            }
            else if(atPosition < 0)
            {
                atPosition = 0;
            }

            var (leftId, rightId) = GetAdjacentCharacterIds(atPosition);

            // Generate new ID between boundaries using standard mid-point
            string newIdStr = GenerateNewId(leftId, rightId);
            _logger?.LogDebug($"Generated new ID: {newIdStr} for character '{character}' at position {atPosition}");
            if (_characterList.ContainsKey(newIdStr))
            {
                    _logger?.LogError($"Composite ID collision: {newIdStr}. This is extremely unlikely and indicates a system error.");
                    throw new Exception($"Composite ID collision: {newIdStr}. This is extremely unlikely and indicates a system error.");       
            }

            var newCharacter = new CRDTCharacterPayload
            {
                Character = character,
                IdCharacter = newIdStr,
                Tombstone = false
            };

            _characterList.Add(newIdStr, newCharacter);
            _sortedList.Add(newIdStr, newCharacter);
            return _characterList[newIdStr];
        }

        /// <summary>
        /// Generate new ID between left and right boundaries using midpoint
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


            return _idService.GenerateIdBetweenComposite(leftIdStr, rightIdStr, _clientId);

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
            foreach (KeyValuePair<string, CRDTCharacterPayload> character in _sortedList)
            {
                if (!character.Value.Tombstone)
                {
                    builder.Append(character.Value.Character);
                }
            }

            return builder.ToString();
        }
        

        public void MergeCharacter(CRDTCharacterPayload c)
        {
            if (!_characterList.ContainsKey(c.IdCharacter))
            {
                _sortedList.Add(c.IdCharacter, c);
                _characterList.Add(c.IdCharacter, c);
            }
            else
            {
                // If the character already exists, we may want to update its tombstone status
                var existingCharacter = _characterList[c.IdCharacter];
                existingCharacter.Tombstone = existingCharacter.Tombstone || c.Tombstone;
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
        private readonly CRDTIdService _idService;

        public CompositeIdComparator(CRDTIdService idService)
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
