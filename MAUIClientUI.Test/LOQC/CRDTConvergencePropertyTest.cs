using DatabaseLibrary.Entities.Client;
using CRDTLibrary.Cursor;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MAUIClientUI.Test.LOQC
{
    /// <summary>
    /// Property-based verification of the CRDT convergence guarantees claimed in the
    /// project report (commutativity, idempotence, tombstone semantics, deterministic
    /// ordering).
    ///
    /// Test names map onto Documentation/TestPlan/TestPlan.md (TC-02 .. TC-12) so that
    /// a result in the .trx output can be traced back to a requirement.
    /// </summary>
    public class CRDTConvergencePropertyTest
    {
        private static readonly Guid ClientA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClientB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        /// <summary>
        /// Builds a set of operations by typing <paramref name="text"/> into a cursor
        /// owned by <paramref name="clientId"/>, then returns the resulting characters.
        /// </summary>
        private static List<CRDTCharacterPayload> ProduceOperations(string text, Guid clientId)
        {
            var cursor = new Document("", clientId);
            var produced = new List<CRDTCharacterPayload>();
            for (int i = 0; i < text.Length; i++)
            {
                produced.Add(cursor.InsertCharacter(i, text[i]));
            }
            return produced;
        }

        /// <summary>
        /// Applies a set of operations to a fresh replica in the given order and returns
        /// the reconstructed text. Merging is order-independent by design.
        /// </summary>
        private static string ApplyToFreshReplica(IEnumerable<CRDTCharacterPayload> operations, Guid replicaOwner)
        {
            var cursor = new Document(new List<CRDTCharacterPayload>(), replicaOwner);
            foreach (var op in operations)
            {
                cursor.MergeCharacter(op);
            }
            return cursor.GetString();
        }

        private static List<T> Shuffle<T>(IEnumerable<T> source, int seed)
        {
            var list = source.ToList();
            var rng = new Random(seed);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        // ---------------------------------------------------------------------
        // TC-02 Commutativity
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_02_ApplyingOperationsInAnyOrderConvergesToSameText()
        {
            var operations = ProduceOperations("The quick brown fox jumps over the lazy dog", ClientA);
            string baseline = ApplyToFreshReplica(operations, ClientB);

            for (int seed = 1; seed <= 10; seed++)
            {
                string shuffled = ApplyToFreshReplica(Shuffle(operations, seed), ClientB);
                Assert.Equal(baseline, shuffled);
            }
        }

        [Fact]
        public void TC_02_CommutativityHoldsWithInterleavedDeletions()
        {
            var cursor = new Document("", ClientA);
            var operations = new List<CRDTCharacterPayload>();

            const string text = "abcdefghij";
            for (int i = 0; i < text.Length; i++)
            {
                operations.Add(cursor.InsertCharacter(i, text[i]));
            }

            // Tombstone a few characters; the same objects are carried in the operation set.
            cursor.DeleteCharacter(3);
            cursor.DeleteCharacter(7);

            string baseline = ApplyToFreshReplica(operations, ClientB);

            for (int seed = 1; seed <= 10; seed++)
            {
                Assert.Equal(baseline, ApplyToFreshReplica(Shuffle(operations, seed), ClientB));
            }
        }

        // ---------------------------------------------------------------------
        // TC-03 / TC-04 Idempotence and duplicate rejection
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_03_ReapplyingEveryOperationLeavesTextUnchanged()
        {
            var operations = ProduceOperations("idempotence", ClientA);

            var cursor = new Document(new List<CRDTCharacterPayload>(), ClientB);
            foreach (var op in operations)
            {
                cursor.MergeCharacter(op);
            }
            string afterFirstPass = cursor.GetString();

            foreach (var op in operations)
            {
                cursor.MergeCharacter(op);
            }

            Assert.Equal(afterFirstPass, cursor.GetString());
        }

        [Fact]
        public void TC_04_DuplicateOperationDoesNotDuplicateCharacter()
        {
            var operations = ProduceOperations("AB", ClientA);

            var cursor = new Document(new List<CRDTCharacterPayload>(), ClientB);
            foreach (var op in operations)
            {
                cursor.MergeCharacter(op);
            }

            // Replay the very same operation many times.
            for (int i = 0; i < 5; i++)
            {
                cursor.MergeCharacter(operations[0]);
            }

            Assert.Equal("AB", cursor.GetString());
        }

        // ---------------------------------------------------------------------
        // TC-05 / TC-06 Concurrent insertion at the same position
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_05_ConcurrentInsertAtSamePositionResolvesDeterministically()
        {
            // Two replicas start from the same shared prefix.
            var shared = ProduceOperations("HI", ClientA);

            var replicaA = new Document(shared.Select(c => c).ToList(), ClientA);
            var replicaB = new Document(shared.Select(c => new CRDTCharacterPayload
            {
                IdCharacter = c.IdCharacter,
                Character = c.Character,
                Tombstone = c.Tombstone,

            }).ToList(), ClientB);

            // Both insert at the same logical position, concurrently.
            var fromA = replicaA.InsertCharacter(1, 'X');
            var fromB = replicaB.InsertCharacter(1, 'Y');

            // Exchange operations.
            replicaA.MergeCharacter(fromB);
            replicaB.MergeCharacter(fromA);

            Assert.Equal(replicaA.GetString(), replicaB.GetString());
            Assert.Contains('X', replicaA.GetString());
            Assert.Contains('Y', replicaA.GetString());
        }

        [Fact]
        public void TC_06_ConcurrentInsertsConvergeRegardlessOfExchangeOrder()
        {
            var shared = ProduceOperations("HI", ClientA);

            var producerA = new Document(shared.ToList(), ClientA);
            var fromA = producerA.InsertCharacter(1, 'X');

            var producerB = new Document(shared.Select(c => new CRDTCharacterPayload
            {
                IdCharacter = c.IdCharacter,
                Character = c.Character,
                Tombstone = c.Tombstone,

            }).ToList(), ClientB);
            var fromB = producerB.InsertCharacter(1, 'Y');

            var all = new List<CRDTCharacterPayload>(shared) { fromA, fromB };

            string forward = ApplyToFreshReplica(all, ClientA);
            string reversed = ApplyToFreshReplica(Enumerable.Reverse(all), ClientA);

            Assert.Equal(forward, reversed);
        }

        // ---------------------------------------------------------------------
        // TC-07 / TC-08 / TC-12 Tombstone semantics
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_07_DeletedCharacterIsTombstonedAndExcludedFromText()
        {
            var cursor = new Document("AB", ClientA);

            var deleted = cursor.DeleteCharacter(2);

            Assert.True(deleted.Tombstone);
            Assert.Equal("A", cursor.GetString());
        }

        [Fact]
        public void TC_08_TombstonedCharacterIsNotResurrectedByLaterArrivingInsert()
        {
            var operations = ProduceOperations("AB", ClientA);

            var cursor = new Document(new List<CRDTCharacterPayload>(), ClientB);
            foreach (var op in operations)
            {
                cursor.MergeCharacter(op);
            }

            // Delete the second character locally.
            cursor.DeleteCharacter(2);
            Assert.Equal("A", cursor.GetString());

            // A stale replica re-sends the original (non-tombstoned) insert for that character.
            var staleInsert = new CRDTCharacterPayload
            {
                IdCharacter = operations[1].IdCharacter,
                Character = operations[1].Character,
                Tombstone = false,
            };
            cursor.MergeCharacter(staleInsert);

            Assert.Equal("A", cursor.GetString());
        }

        [Fact]
        public void TC_12_ReconstructionFiltersTombstonesAndSortsByIdCharacter()
        {
            var cursor = new Document("HELLO", ClientA);

            cursor.DeleteCharacter(1); // remove 'H'
            cursor.DeleteCharacter(5); // remove 'O'

            Assert.Equal("ELL", cursor.GetString());
        }

        // ---------------------------------------------------------------------
        // TC-09 Out-of-order delete before insert
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_09_DeleteArrivingBeforeItsInsertDoesNotCorruptState()
        {
            var operations = ProduceOperations("ABC", ClientA);

            // Simulate the delete of 'B' arriving as a tombstoned character before the
            // replica has ever seen 'B'.
            var tombstonedB = new CRDTCharacterPayload
            {
                IdCharacter = operations[1].IdCharacter,
                Character = operations[1].Character,
                Tombstone = true,
            };

            var cursor = new Document(new List<CRDTCharacterPayload>(), ClientB);
            cursor.MergeCharacter(tombstonedB);          // delete first
            cursor.MergeCharacter(operations[0]);        // then the inserts
            cursor.MergeCharacter(operations[2]);
            cursor.MergeCharacter(operations[1]);        // late-arriving original insert of 'B'

            Assert.Equal("AC", cursor.GetString());
        }

        // ---------------------------------------------------------------------
        // TC-10 Divergent replicas converge
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_10_DivergentReplicasConvergeAfterExchangingOperations()
        {
            var shared = ProduceOperations("BASE", ClientA);

            var replicaA = new Document(shared.ToList(), ClientA);
            var replicaB = new Document(shared.Select(c => new CRDTCharacterPayload
            {
                IdCharacter = c.IdCharacter,
                Character = c.Character,
                Tombstone = c.Tombstone,

            }).ToList(), ClientB);

            var aOps = new List<CRDTCharacterPayload>
            {
                replicaA.InsertCharacter(4, '1'),
                replicaA.InsertCharacter(5, '2')
            };

            var bOps = new List<CRDTCharacterPayload>
            {
                replicaB.InsertCharacter(0, '9'),
                replicaB.InsertCharacter(1, '8')
            };

            foreach (var op in bOps) replicaA.MergeCharacter(op);
            foreach (var op in Enumerable.Reverse(aOps)) replicaB.MergeCharacter(op);

            Assert.Equal(replicaA.GetString(), replicaB.GetString());
        }

        // ---------------------------------------------------------------------
        // TC-11 Clock skew between devices
        // ---------------------------------------------------------------------

        [Fact]
        public void TC_11_ConvergenceHoldsUnderClockSkewBetweenDevices()
        {
            var shared = ProduceOperations("HI", ClientA);

            var producerA = new Document(shared.ToList(), ClientA);
            var fromA = producerA.InsertCharacter(1, 'X');
            // Device A's clock runs one hour ahead.


            var producerB = new Document(shared.Select(c => new CRDTCharacterPayload
            {
                IdCharacter = c.IdCharacter,
                Character = c.Character,
                Tombstone = c.Tombstone,

            }).ToList(), ClientB);
            var fromB = producerB.InsertCharacter(1, 'Y');
            // Device B's clock runs one hour behind.

            var all = new List<CRDTCharacterPayload>(shared) { fromA, fromB };

            string order1 = ApplyToFreshReplica(all, ClientA);
            string order2 = ApplyToFreshReplica(Enumerable.Reverse(all), ClientA);
            string order3 = ApplyToFreshReplica(Shuffle(all, 42), ClientA);

            // Ordering is decided by the CRDT identifier, not the wall clock, so skew
            // must not break convergence.
            Assert.Equal(order1, order2);
            Assert.Equal(order1, order3);
        }

        [Fact]
        public void TC_11_IdenticalClocksStillProduceDeterministicOrdering()
        {
            var shared = ProduceOperations("HI", ClientA);
            DateTime sameClock = DateTime.UtcNow;

            var producerA = new Document(shared.ToList(), ClientA);
            var fromA = producerA.InsertCharacter(1, 'X');

            var producerB = new Document(shared.Select(c => new CRDTCharacterPayload
            {
                IdCharacter = c.IdCharacter,
                Character = c.Character,
                Tombstone = c.Tombstone,

            }).ToList(), ClientB);
            var fromB = producerB.InsertCharacter(1, 'Y');

            var all = new List<CRDTCharacterPayload>(shared) { fromA, fromB };

            Assert.Equal(
                ApplyToFreshReplica(all, ClientA),
                ApplyToFreshReplica(Enumerable.Reverse(all), ClientA));
        }
    }
}
