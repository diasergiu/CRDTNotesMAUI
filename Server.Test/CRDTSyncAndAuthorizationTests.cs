using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Server;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Server.ServeRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Server.Test
{
    /// <summary>
    /// Integration tests for CRDT merge, synchronisation, authorisation and durability.
    ///
    /// These use a SQLite in-memory database rather than the EF in-memory provider,
    /// because <see cref="NotesRepository.UpdateChanges"/> opens a real transaction and
    /// <see cref="NotesRepository.DeleteCharacters"/> uses ExecuteDelete; neither is
    /// supported by the in-memory provider.
    ///
    /// Test names map onto Documentation/TestPlan/TestPlan.md (TC-13 .. TC-35).
    /// </summary>
    public class CRDTSyncAndAuthorizationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DbContextServer> _options;
        private readonly DbContextServer _dbContext;
        private readonly NotesRepository _repository;

        private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid UserC = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        public CRDTSyncAndAuthorizationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<DbContextServer>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new DbContextServer(_options);
            _dbContext.Database.EnsureCreated();
            _repository = new NotesRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }

        private DbContextServer NewContext() => new DbContextServer(_options);

        private async Task<UserServer> SeedUserAsync(Guid id, string username)
        {
            var user = new UserServer
            {
                IdUser = id,
                Name = username,
                Username = username,
                Password = "pw-" + username
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<NoteServer> SeedNoteAsync(Guid owner, string title = "Note")
        {
            var note = new NoteServer
            {
                IdNote = Guid.NewGuid(),
                Title = title,
                Content = "",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1
            };
            return await _repository.CreateNote(note, owner);
        }

        private static CRDTCharacter Char(Guid noteId, string id, char value, bool tombstone = false)
            => new CRDTCharacter
            {
                IdCharacter = id,
                IdNote = noteId,
                Character = value,
                Operation = tombstone ? "delete" : "insert",
                Tombstone = tombstone,
                ClockDateTime = DateTime.UtcNow
            };

        private static string Reconstruct(IEnumerable<CRDTCharacter> characters)
            => new string(characters
                .Where(c => !c.Tombstone)
                .OrderBy(c => c.IdCharacter, StringComparer.Ordinal)
                .Select(c => c.Character)
                .ToArray());

        // -----------------------------------------------------------------
        // TC-13 Note creation and listing
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_13_CreatedNoteAppearsInOwnersNoteList()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA, "My first note");

            var notes = await _repository.GetAllNotesFromUser(UserA);

            Assert.Contains(notes, n => n.IdNote == note.IdNote && n.Title == "My first note");
        }

        // -----------------------------------------------------------------
        // TC-14 Merge of concurrent batches from two devices
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_14_OperationsFromTwoDevicesAreBothRetainedAfterMerge()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA);

            var deviceA = new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'H'),
                Char(note.IdNote, "(2),(a)", 'I')
            };
            var deviceB = new List<CRDTCharacter>
            {
                Char(note.IdNote, "(3),(b)", '!'),
            };

            await _repository.saveCRDTChanges(deviceA);
            await _repository.saveCRDTChanges(deviceB);

            var stored = await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserA);

            Assert.Equal(3, stored.Count);
            Assert.Equal("HI!", Reconstruct(stored));
        }

        // -----------------------------------------------------------------
        // TC-15 Replaying an identical batch must not duplicate
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_15_ReplayingIdenticalBatchDoesNotDuplicateCharacters()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA);

            var batch = new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'A'),
                Char(note.IdNote, "(2),(a)", 'B')
            };

            await _repository.saveCRDTChanges(batch);
            int countAfterFirst = (await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserA)).Count;

            // Re-submit the very same operations, as would happen if an acknowledgement
            // was lost and the client retried. Idempotence (FR-14) requires this to be
            // harmless.
            var replay = new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'A'),
                Char(note.IdNote, "(2),(a)", 'B')
            };

            var replayException = await Record.ExceptionAsync(() => _repository.saveCRDTChanges(replay));

            Assert.True(replayException == null,
                "FR-14 violated: replaying an already-acknowledged batch threw " +
                $"{replayException?.GetType().Name}. saveCRDTChanges looks up existing rows with " +
                "AsNoTracking() and then calls Update() on the caller's instances, so a retried " +
                "batch causes an EF identity conflict instead of being absorbed idempotently. " +
                $"Message: {replayException?.Message}");

            var stored = await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserA);

            Assert.Equal(countAfterFirst, stored.Count);
            Assert.Equal("AB", Reconstruct(stored));
        }

        // -----------------------------------------------------------------
        // TC-16 Out-of-order submission converges
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_16_OutOfOrderSubmissionProducesIdenticalFinalState()
        {
            await SeedUserAsync(UserA, "alice");
            var noteInOrder = await SeedNoteAsync(UserA, "in-order");
            var noteReversed = await SeedNoteAsync(UserA, "reversed");

            List<CRDTCharacter> Ops(Guid id) => new List<CRDTCharacter>
            {
                Char(id, "(1),(a)", 'C'),
                Char(id, "(2),(a)", 'R'),
                Char(id, "(3),(a)", 'D'),
                Char(id, "(4),(a)", 'T')
            };

            var forward = Ops(noteInOrder.IdNote);
            var reversed = Ops(noteReversed.IdNote);
            reversed.Reverse();

            await _repository.saveCRDTChanges(forward);
            await _repository.saveCRDTChanges(reversed);

            var a = Reconstruct(await _repository.getCRDTCharactersbyIdNote(noteInOrder.IdNote, UserA));
            var b = Reconstruct(await _repository.getCRDTCharactersbyIdNote(noteReversed.IdNote, UserA));

            Assert.Equal(a, b);
            Assert.Equal("CRDT", a);
        }

        // -----------------------------------------------------------------
        // TC-17 Reconstruction excludes tombstones
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_17_ReconstructedTextExcludesTombstonedCharacters()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA);

            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'H'),
                Char(note.IdNote, "(2),(a)", 'X', tombstone: true),
                Char(note.IdNote, "(3),(a)", 'I')
            });

            var stored = await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserA);

            Assert.Equal("HI", Reconstruct(stored));
            Assert.Contains(stored, c => c.Tombstone && c.Character == 'X');
        }

        // -----------------------------------------------------------------
        // TC-18 Title update and deletion
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_18_TitleUpdateSucceedsWhenVersionMatches()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA, "Original");

            // CreateNote returns a tracked entity whose Version is incremented in place by
            // UpdateChanges, so capture the original value first.
            int originalVersion = note.Version;

            var update = new NoteServer
            {
                IdNote = note.IdNote,
                Title = "Renamed",
                Content = "",
                CreationDate = note.CreationDate,
                LastUpdate = DateTime.UtcNow,
                Version = originalVersion
            };

            var result = await _repository.UpdateChanges(update, UserA);

            Assert.True(result.IsSuccess);
            Assert.False(result.IsVersionConflict);
            Assert.Equal("Renamed", result.ServerNote.Title);
            Assert.Equal(originalVersion + 1, result.ServerNote.Version);
        }

        [Fact]
        public async Task TC_18_DeletingNoteRemovesItFromOwnersList()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA);

            await _repository.DeleteNote(note.IdNote, UserA);

            var notes = await _repository.GetAllNotesFromUser(UserA);
            Assert.DoesNotContain(notes, n => n.IdNote == note.IdNote);
        }

        // -----------------------------------------------------------------
        // TC-19 Optimistic concurrency conflict
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_19_StaleVersionUpdateReturnsVersionConflict()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA, "Original");

            // Both writers start from this version; capture it before the first update
            // mutates the tracked entity.
            int originalVersion = note.Version;

            // First update moves the note to the next version.
            await _repository.UpdateChanges(new NoteServer
            {
                IdNote = note.IdNote,
                Title = "First writer",
                Content = "",
                CreationDate = note.CreationDate,
                LastUpdate = DateTime.UtcNow,
                Version = originalVersion
            }, UserA);

            // Second writer still believes the note is at the original version.
            var stale = await _repository.UpdateChanges(new NoteServer
            {
                IdNote = note.IdNote,
                Title = "Second writer",
                Content = "",
                CreationDate = note.CreationDate,
                LastUpdate = DateTime.UtcNow,
                Version = originalVersion
            }, UserA);

            Assert.True(stale.IsVersionConflict);
            Assert.NotNull(stale.ServerNote);
            Assert.Equal("First writer", stale.ServerNote.Title);
        }

        // -----------------------------------------------------------------
        // TC-23 Sharing grants access
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_23_SharedNoteAppearsInCollaboratorsNoteList()
        {
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA, "Shared note");

            Assert.DoesNotContain(await _repository.GetAllNotesFromUser(UserB),
                n => n.IdNote == note.IdNote);

            await _repository.SaveNoteUserConnection(note.IdNote, UserB);

            Assert.Contains(await _repository.GetAllNotesFromUser(UserB),
                n => n.IdNote == note.IdNote);
        }

        [Fact]
        public async Task TC_23_CollaboratorCanSubmitOperationsThatOwnerObserves()
        {
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA);
            await _repository.SaveNoteUserConnection(note.IdNote, UserB);

            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'O')
            });
            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(2),(b)", 'K')
            });

            var ownerView = await _repository.GetAllCRDTByUser(UserA);
            var collaboratorView = await _repository.GetAllCRDTByUser(UserB);

            Assert.Equal("OK", Reconstruct(ownerView));
            Assert.Equal("OK", Reconstruct(collaboratorView));
        }

        // -----------------------------------------------------------------
        // TC-20 / TC-21 / TC-22 Unauthorised access
        //
        // These encode the SPECIFIED permission model documented in
        // Documentation/Requirements/UseCases.md. Gaps G-01, G-02 and G-04 mean the
        // current implementation does not enforce it, so these tests are expected to
        // fail until the authorisation checks are restored. They are retained
        // deliberately as objective evidence of the gap.
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_20_UserWithoutGrantCannotReadAnotherUsersNote()
        {  // The access check is done at the repository level but the application shuld check at the controller level. The controller check is commented out, so this test fails.
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA, "Alice private");

            var fetched = await _repository.GetNoteById(note.IdNote, UserB);

            Assert.True(fetched == null,
                "SEC-03 violated (gap G-01): GetNoteById returned a note to a user with no " +
                "Note_Users grant. The access check in NotesRepository.GetNoteById is commented out.");
        }

        [Fact]
        public async Task TC_20_NoteIsNotListedForUserWithoutGrant()
        {
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA, "Alice private");

            var bobsNotes = await _repository.GetAllNotesFromUser(UserB);

            Assert.DoesNotContain(bobsNotes, n => n.IdNote == note.IdNote);
        }

        [Fact]
        public async Task TC_21_UserWithoutGrantCannotReadCharacterLogOfAnotherUsersNote()
        {
            // The access check is done at the repository level but the application shuld check at the controller level. The controller check is commented out, so this test fails.
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA, "Alice private");

            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'S'),
                Char(note.IdNote, "(2),(a)", 'E')
            });

            // GetAllCRDTByUser is the access-scoped query and does enforce the grant.
            var scoped = await _repository.GetAllCRDTByUser(UserB);
            Assert.Empty(scoped);

            // getCRDTCharactersbyIdNote is the query the controller actually calls, and
            // it takes no user parameter at all (gap G-02).
            var unscoped = await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserB);
            Assert.True(unscoped.Count == 0,
                "SEC-03 violated (gap G-02): getCRDTCharactersbyIdNote exposes the full " +
                "character log - and therefore the full note text - without any access check.");
        }

        [Fact]
        public async Task TC_22_OperationsFromUnauthorisedUserAreNotMerged()
        {
            // The access check is done at the repository level but the application shuld check at the controller level. The controller check is commented out, so this test fails.
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA, "Alice private");

            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'A')
            });

            // Bob has no grant for this note but submits an operation for it.
            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(2),(b)", 'X')
            });

            var stored = await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserA);

            Assert.True(stored.All(c => c.Character != 'X'),
                "SEC-06 violated (gap G-04): saveCRDTChanges merged an operation from a user " +
                "with no access grant for the target note.");
        }

        [Fact]
        public async Task TC_25_CollaboratorCannotDeleteOwnersNote()
        {
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA, "Alice note");
            await _repository.SaveNoteUserConnection(note.IdNote, UserB);

            await _repository.DeleteNote(note.IdNote, UserB);

            var stillExists = await NewContext().Notes.AnyAsync(n => n.IdNote == note.IdNote);

            Assert.True(stillExists,
                "SEC-02 violated (gap G-05): DeleteNote checks only for a Note_Users association, " +
                "so a collaborator can delete a note they do not own. NoteServer has no " +
                "CreatedByUserId field (gap G-08), so ownership cannot currently be checked.");
        }

        [Fact]
        public async Task TC_24_NonOwnerCannotGrantAccessToAnotherUser()
        {
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            await SeedUserAsync(UserC, "carol");
            var note = await SeedNoteAsync(UserA, "Alice note");
            await _repository.SaveNoteUserConnection(note.IdNote, UserB);

            // Bob, a mere collaborator, grants Carol access. There is no API on the
            // sharing path that even accepts the identity of the granting user, so the
            // ownership rule in SEC-04 cannot be expressed, let alone enforced.
            await _repository.SaveNoteUserConnection(note.IdNote, UserC);

            var carolsNotes = await _repository.GetAllNotesFromUser(UserC);

            Assert.True(carolsNotes.All(n => n.IdNote != note.IdNote),
                "SEC-04 violated (gaps G-03 and G-08): a non-owner successfully granted a third " +
                "user access. SaveNoteUserConnection(noteId, userId) takes no granting-user " +
                "parameter, and NotesController.GiveNoteAccessToUser has its requester lookup " +
                "commented out, so any authenticated user can share any note.");
        }

        // -----------------------------------------------------------------
        // TC-33 Durability across context lifetime
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_33_AcknowledgedOperationsSurviveContextRecreation()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA);

            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'D'),
                Char(note.IdNote, "(2),(a)", 'U'),
                Char(note.IdNote, "(3),(a)", 'R')
            });

            // Simulate a server restart: dispose the working context and read through a
            // brand new one against the same underlying database.
            using var freshContext = NewContext();
            var freshRepository = new NotesRepository(freshContext);

            var stored = await freshRepository.getCRDTCharactersbyIdNote(note.IdNote, UserA);

            Assert.Equal(3, stored.Count);
            Assert.Equal("DUR", Reconstruct(stored));
        }

        [Fact]
        public async Task TC_33_NoteMetadataSurvivesContextRecreation()
        {
            await SeedUserAsync(UserA, "alice");
            var note = await SeedNoteAsync(UserA, "Durable title");

            using var freshContext = NewContext();
            var reloaded = await freshContext.Notes.FirstOrDefaultAsync(n => n.IdNote == note.IdNote);

            Assert.NotNull(reloaded);
            Assert.Equal("Durable title", reloaded.Title);
        }

        // -----------------------------------------------------------------
        // TC-35 Delayed reconnection converges
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_35_LateArrivingOfflineOperationsConvergeWithOnlineEdits()
        {
            await SeedUserAsync(UserA, "alice");
            await SeedUserAsync(UserB, "bob");
            var note = await SeedNoteAsync(UserA);
            await _repository.SaveNoteUserConnection(note.IdNote, UserB);

            // Bob is online and edits first.
            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(2),(b)", 'B'),
                Char(note.IdNote, "(4),(b)", 'D')
            });

            // Alice was offline the whole time and only now flushes her queue.
            await _repository.saveCRDTChanges(new List<CRDTCharacter>
            {
                Char(note.IdNote, "(1),(a)", 'A'),
                Char(note.IdNote, "(3),(a)", 'C')
            });

            var stored = await _repository.getCRDTCharactersbyIdNote(note.IdNote, UserA);

            Assert.Equal(4, stored.Count);
            Assert.Equal("ABCD", Reconstruct(stored));
        }
    }
}
