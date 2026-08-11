using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MAUIClientUI.Cursor
{
    /// <summary>
    /// LSEQ (Logoot-like) ID generation with conflict resolution
    /// Handles concurrent inserts at same position by same two users
    /// </summary>
    public class LseqIdService
    {
        private readonly Guid _localClientId;
        private const int MAX_PRECISION = 1000000; // Decimal places to avoid infinite subdiv

        public LseqIdService(Guid clientId)
        {
            _localClientId = clientId;
        }

        /// <summary>
        /// Generate unique ID between two boundaries
        /// Handles conflict when two users pick same ID
        /// </summary>
        public decimal 
            GenerateIdBetween(decimal? leftId, decimal? rightId, Guid clientId)
        {
            return GenerateIdBetweenInternal(leftId, rightId, clientId, depth: 1);
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
                    //+ (decimal) Math.Pow(10, -depth + 1);

            // Case 4: Insert between two characters
            decimal gap = rightId.Value - leftId.Value;

            // Enough space? Use midpoint
            //if (gap > minGapAtDepth) // Arbitrary threshold for "enough space"
            //{
            return leftId.Value + (gap / 2);
            //}

            // Not enough space: use fractional approach
            // CONFLICT RESOLUTION: Use clientId as tiebreaker
          //  decimal newId = leftId.Value + ((clientId % 10) / (decimal)Math.Pow(10, depth + 1));

         
            // ID collision detected - recurse to next precision level
            //return GenerateIdBetweenInternal(leftId, rightId, clientId, depth + 1);
            

        }

        /// <summary>
        /// ADVANCED: Handle simultaneous insert conflict using ClientId ordering
        /// This is called when server detects two operations with same generated ID
        /// </summary>
        public bool ShouldAcceptConflictingInsert(
            CRDTCharacterClient existingChar,
            CRDTCharacterClient newChar,
            DateTime existingTimestamp,
            DateTime newTimestamp)
        {
            // Same ID from different clients
            if (existingChar.IdCharacter != newChar.IdCharacter)
                return true; // No conflict

            // Tiebreaker 1: By ClientId (deterministic)
            // Lower ClientId takes precedence
            if (existingChar.ClientId != newChar.ClientId)
                return existingChar.ClientId < newChar.ClientId;

            // Tiebreaker 2: By Timestamp (causal ordering)
            if (existingTimestamp != newTimestamp)
                return existingTimestamp < newTimestamp;

            // Should never reach here - same ID, same client, same timestamp is invalid
            throw new InvalidOperationException(
                $"Duplicate insert detected: " +
                $"Client {existingChar.ClientId} at {existingTimestamp.Ticks}");
        }

        /// <summary>
        /// Detect and resolve ID collisions in a batch of concurrent operations
        /// </summary>
    //    public List<CRDTCharacterClient> ResolveBatchConflicts(
    //        List<CRDTCharacterClient> existingChars,
    //        List<CRDTCharacterClient> incomingChars)
    //    {
    //        var conflicts = new Dictionary<int, List<CRDTCharacterClient>>();

    //        // Group by ID to find collisions
    //        foreach (var ch in existingChars.Concat(incomingChars))
    //        {
    //            if (!conflicts.ContainsKey(ch.IdCharacter))
    //                conflicts[ch.IdCharacter] = new List<CRDTCharacterClient>();
    //            conflicts[ch.IdCharacter].Add(ch);
    //        }

    //        var resolved = new List<CRDTCharacterClient>();

    //        // Process each potential conflict group
    //        foreach (var group in conflicts.Values)
    //        {
    //            if (group.Count == 1)
    //            {
    //                // No conflict
    //                resolved.Add(group[0]);
    //            }
    //            else
    //            {
    //                // Conflict: Sort by ClientId, then Timestamp, take first
    //                var winner = group
    //                    .OrderBy(c => c.ClientId)
    //                    .ThenBy(c => DateTime.Parse(c.ClockDateTime))
    //                    .First();

    //                resolved.Add(winner);

    //                // Mark losers as tombstone
    //                foreach (var loser in group.Except(new[] { winner }))
    //                {
    //                    loser.Tombstone = true;
    //                    resolved.Add(loser);
    //                }
    //            }
    //        }

    //        return resolved;
    //    }
    }
}
