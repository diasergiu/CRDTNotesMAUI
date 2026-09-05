
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("MAUIClientUI.Test")]

namespace CRDTLibrary.Cursor
{
    /// <summary>
    /// (Logoot-like) ID generation with conflict resolution
    /// Handles concurrent inserts at same position by same two users
    /// Supports both simple decimal IDs and composite IDs: (pos,site)(pos,site)...
    /// </summary>
    public class CRDTIdService
    {
        private readonly Guid _localClientId;
        private const int SAFE_DECIMAL_DIGITS = 25;

        internal static readonly decimal MinGapThreshold = (decimal)Math.Pow(10, -SAFE_DECIMAL_DIGITS);
        private const string COMPOSITE_ID_PATTERN = @"\((.*?),(.*?)\)"; // Pattern: (position,clientId)

        public CRDTIdService(Guid clientId)
        {
            _localClientId = clientId;
        }


        /// <summary>
        /// Generate composite ID string between two boundaries
        /// When left and right positions are equal, adds new nesting level
        /// Format: (position,site)(position,site)...
        /// </summary>
        public string GenerateIdBetweenComposite(string leftId, string rightId, Guid clientId)
        {
            // Parse the left and right IDs into component arrays
            var leftComponents = ParseCompositeId(leftId);
            var rightComponents = ParseCompositeId(rightId);

            // Find the first level where positions differ, or determine nesting level
            int minLength = Math.Min(leftComponents.Count, rightComponents.Count);
            int nestingLevel = 0;
            var resultComponents = new List<IdComponent>();
            // Find where components differ
            for (int i = 0; i < minLength; i++)
            {
                if (leftComponents[i].Position != rightComponents[i].Position && !(rightComponents[i].Position - leftComponents[i].Position <= MinGapThreshold))
                {
                    // Positions differ at this level - generate new position between them
                    nestingLevel = i;
                    break;
                }
                resultComponents.Add(leftComponents[i]); // add left is they are ==
                nestingLevel = i + 1;
            }

            // Build result components up to and including the nesting level
            
            decimal? leftBoundary = nestingLevel < leftComponents.Count ? leftComponents[nestingLevel].Position : null;
            decimal? rightBoundary = nestingLevel < rightComponents.Count ? rightComponents[nestingLevel].Position : null;

            // Generate new position between left and right at this nesting level
            decimal newPosition = GenerateIdBetween(
                leftBoundary,
                rightBoundary,
                clientId);

            resultComponents.Add(new IdComponent { Position = newPosition, SiteId = clientId });
        

            return BuildCompositeIdString(resultComponents);
        }

        /// <summary>
        /// Parse composite ID string into component array
        /// Input format: "(position,clientId)(position,clientId)..."
        /// Returns: list of IdComponent with position and site values
        /// </summary>
        public List<IdComponent> ParseCompositeId(string compositeIdString)
        {
            if (string.IsNullOrEmpty(compositeIdString))
                return new List<IdComponent>();

            var components = new List<IdComponent>();
            var matches = Regex.Matches(compositeIdString, COMPOSITE_ID_PATTERN);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    string positionStr = match.Groups[1].Value.Trim();
                    string siteStr = match.Groups[2].Value.Trim();

                    if (decimal.TryParse(positionStr, out decimal position) &&
                        Guid.TryParse(siteStr, out Guid siteId))
                    {
                        components.Add(new IdComponent { Position = position, SiteId = siteId });
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// Build composite ID string from components
        /// Format: "(position,clientId)(position,clientId)..."
        /// </summary>
        public string BuildCompositeIdString(IEnumerable<IdComponent> components)
        {
            return string.Concat(components.Select(c => $"({c.Position},{c.SiteId})"));
        }

        /// <summary>
        /// Check if a string is a composite ID (contains pattern with parentheses)
        /// </summary>
        public bool IsCompositeId(string idString)
        {
            if (string.IsNullOrEmpty(idString))
                return false;

            return Regex.IsMatch(idString, COMPOSITE_ID_PATTERN);
        }

        /// <summary>
        /// Convert composite ID to a comparable string for sorting
        /// Ensures consistent ordering across clients
        /// </summary>
        public string CompositeIdToOrderableString(string compositeId)
        {
            var components = ParseCompositeId(compositeId);
            if (components.Count == 0)
                return compositeId;

            // Create a sortable representation: pad positions and include sites
            return string.Concat(components.Select(c => 
                $"{c.Position.ToString("D20")},{c.SiteId}|"));
        }

        /// <summary>
        /// Represents a single component in a composite CRDT ID
        /// </summary>
        public class IdComponent
        {
            public decimal Position { get; set; }
            public Guid SiteId { get; set; }

            public override string ToString() => $"({Position},{SiteId})";
        }

        internal decimal GenerateIdBetween(decimal? leftId, decimal? rightId, Guid clientId)
        {
         

            // Case 1: Insert at start
            if (leftId == null && rightId == null)
                return 1;

            // Case 2: Insert before first character
            if (leftId == null)
                return rightId.Value / 2;

            // Case 3: Insert after last character
            if (rightId == null)    
                return leftId.Value + 1;

            // Case 4: Insert between two characters
            decimal gap = rightId.Value - leftId.Value;

            if (gap <= MinGapThreshold)
            {

                throw new InvalidOperationException(
                    $"Cannot generate ID: gap {gap} between {leftId} and {rightId} is below the minimum " +
                    $"safe threshold ({MinGapThreshold}). A new nesting level is required.");
            }

            // Generate midpoint between left and right
            return leftId.Value + (gap / 2);
        }

    }
}
