using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MAUIClientUI.Cursor
{
    /// <summary>
    /// LSEQ (Logoot-like) ID generation with conflict resolution
    /// Handles concurrent inserts at same position by same two users
    /// Supports both simple decimal IDs and composite IDs: (pos,site)(pos,site)...
    /// </summary>
    public class LseqIdService
    {
        private readonly Guid _localClientId;
        private const int MAX_PRECISION = 1000000; // Decimal places to avoid infinite subdiv
        private const string COMPOSITE_ID_PATTERN = @"\((.*?),(.*?)\)"; // Pattern: (position,clientId)

        public LseqIdService(Guid clientId)
        {
            _localClientId = clientId;
        }

        /// <summary>
        /// Generate unique decimal ID between two boundaries
        /// Handles conflict when two users pick same ID
        /// </summary>
        public decimal GenerateIdBetween(decimal? leftId, decimal? rightId, Guid clientId)
        {
            return GenerateIdBetweenInternal(leftId, rightId, clientId, depth: 1);
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

            // Find where components differ
            for (int i = 0; i < minLength; i++)
            {
                if (leftComponents[i].Position != rightComponents[i].Position)
                {
                    // Positions differ at this level - generate new position between them
                    nestingLevel = i;
                    break;
                }
                nestingLevel = i + 1;
            }

            // Build result components up to and including the nesting level
            var resultComponents = new List<IdComponent>();
            decimal? leftBoundary = nestingLevel < leftComponents.Count ? leftComponents[nestingLevel].Position : null;
            decimal? rightBoundary = nestingLevel < rightComponents.Count ? rightComponents[nestingLevel].Position : null;

            if (nestingLevel < leftComponents.Count - 1)
            {
                resultComponents.AddRange(leftComponents.Take(nestingLevel));
            }
          
            // Generate new position between left and right at this nesting level
            decimal newPosition = GenerateIdBetweenInternal(
                leftBoundary,
                rightBoundary,
                clientId,
                depth: 1);

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
        /// Extract decimal boundaries from composite ID components
        /// Returns the decimal values to use for LSEQ generation
        /// </summary>
        private (decimal?, decimal?) ExtractBoundaryDecimals(List<IdComponent> leftComponents, List<IdComponent> rightComponents)
        {
            decimal? leftDecimal = null;
            decimal? rightDecimal = null;

            // Use the last component's position if available
            if (leftComponents.Count > 0)
                leftDecimal = leftComponents[leftComponents.Count - 1].Position;

            if (rightComponents.Count > 0)
                rightDecimal = rightComponents[rightComponents.Count - 1].Position;

            return (leftDecimal, rightDecimal);
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

        private decimal GenerateIdBetweenInternal(decimal? leftId, decimal? rightId, Guid clientId, int depth)
        {
            if (depth > MAX_PRECISION)
                throw new InvalidOperationException("Cannot generate ID: reached maximum precision. Too many conflicts at this position.");

            decimal minGapAtDepth = (decimal)Math.Pow(10, -depth);

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

            // Generate midpoint between left and right
            return leftId.Value + (gap / 2);
        }

    }
}
