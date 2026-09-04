using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MAUIClientUI.Test.Repository
{
    /// <summary>
    /// Unit tests for <see cref="NoteRepository"/> exercising the real client SQLite
    /// database through <see cref="DbContextClient"/>. Each test instance uses a unique
    /// database file that is deleted on Dispose.
    /// </summary>
    public class NoteRepositoryTest : IDisposable
    {
        private readonly string _instanceId;
        private readonly Guid _noteId = Guid.NewGuid();

        public NoteRepositoryTest()
        {
            _instanceId = "-test-" + Guid.NewGuid().ToString("N");
            using var context = NewContext();
            context.Database.EnsureCreated();
        }

        private DbContextClient NewContext() => new DbContextClient(_instanceId);

        public void Dispose()
        {
            try
            {
                using var context = NewContext();
                var path = context.DbPath;
                context.Database.EnsureDeleted();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static NoteClient BuildNote(Guid id, string title = "note", bool dirty = false, bool deleted = false) =>
            new NoteClient
            {
                IdNote = id,
                Title = title,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1,
                DirtyFlagChangesMade = dirty,
                isDeleted = deleted
            };

        private static NoteServer BuildServerNote(Guid id, string title = "server-note",
            List<CRDTCharacterServer>? characters = null) =>
            new NoteServer
            {
                IdNote = id,
                Title = title,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1,
                DirtyFlagChangesMade = false,
                isDeleted = false,
                CRDTCharacter = characters
            };

        private static CRDTCharacterClient BuildChar(Guid noteId, string idCharacter, char value, bool dirty = true, bool tombstone = false) =>
            new CRDTCharacterClient
            {
                IdCharacter = idCharacter,
                IdNote = noteId,
                Character = value,
                Tombstone = tombstone,
                ClockDateTime = DateTime.UtcNow,
                IsDirtyFlag = dirty
            };

        private static string EncodePayload(Guid noteId, params (string id, char ch)[] characters)
        {
            var list = characters
                .Select(c => new CRDTCharacterClient
                {
                    IdCharacter = c.id,
                    IdNote = noteId,
                    Character = c.ch,
                    Tombstone = false,
                    IsDirtyFlag = false
                })
                .ToList();
            return CharacterSerializer.Encode(list);
        }

        // ==================================================================
        // UpdateBasedOnNoteServer
        // ==================================================================
        #region UpdateBasedOnNoteServer

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithEmptyList_DoesNotThrowAndPersistsNothing()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            await repository.UpdateBasedOnNoteServer(new List<NoteServer>());

            using var verify = NewContext();
            Assert.Empty(verify.Notes.ToList());
            Assert.Empty(verify.CRDTCharacters.ToList());
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithNullCRDTCharacters_PersistsNoteButNoCharacters()
        {
            var serverNote = BuildServerNote(_noteId, "no-chars", characters: null);

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.UpdateBasedOnNoteServer(new List<NoteServer> { serverNote });
            }

            using var verify = NewContext();
            var persisted = verify.Notes.ToList();
            Assert.Single(persisted);
            Assert.Equal(_noteId, persisted[0].IdNote);
            Assert.Empty(verify.CRDTCharacters.ToList());
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithValidEncodedPayload_DecodesAndPersistsCharacters()
        {
            var payload = EncodePayload(_noteId, ("(1),(a)", 'H'), ("(2),(a)", 'i'));
            var serverNote = BuildServerNote(_noteId, "hi", new List<CRDTCharacterServer>
            {
                new CRDTCharacterServer { IdNote = _noteId, Payload = payload }
            });

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.UpdateBasedOnNoteServer(new List<NoteServer> { serverNote });
            }

            using var verify = NewContext();
            var characters = verify.CRDTCharacters.OrderBy(c => c.IdCharacter).ToList();
            Assert.Equal(2, characters.Count);
            Assert.All(characters, c => Assert.Equal(_noteId, c.IdNote));
            Assert.All(characters, c => Assert.True(c.IsDirtyFlag));
            Assert.Contains(characters, c => c.Character == 'H');
            Assert.Contains(characters, c => c.Character == 'i');
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithBadlyEncryptedPayload_PersistsNoteAndSkipsCharacters()
        {
            // A syntactically-valid Base64 string that is NOT a valid encrypted CRDT payload.
            // CharacterSerializer.Decode swallows the exception and returns an empty list.
            var badPayload = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var serverNote = BuildServerNote(_noteId, "corrupted", new List<CRDTCharacterServer>
            {
                new CRDTCharacterServer { IdNote = _noteId, Payload = badPayload }
            });

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.UpdateBasedOnNoteServer(new List<NoteServer> { serverNote });
            }

            using var verify = NewContext();
            Assert.Single(verify.Notes.ToList());
            Assert.Empty(verify.CRDTCharacters.ToList());
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithGarbagePayload_DoesNotThrow()
        {
            var serverNote = BuildServerNote(_noteId, "garbage", new List<CRDTCharacterServer>
            {
                new CRDTCharacterServer { IdNote = _noteId, Payload = "!!!not-base64!!!" }
            });

            using var context = NewContext();
            var repository = new NoteRepository(context);

            var exception = await Record.ExceptionAsync(() =>
                repository.UpdateBasedOnNoteServer(new List<NoteServer> { serverNote }));

            Assert.Null(exception);
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithNullNoteInList_Throws()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                repository.UpdateBasedOnNoteServer(new List<NoteServer> { null! }));
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithMultipleNotes_PersistsAll()
        {
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var payloadA = EncodePayload(idA, ("(1),(a)", 'A'));
            var payloadB = EncodePayload(idB, ("(1),(b)", 'B'), ("(2),(b)", 'B'));

            var notes = new List<NoteServer>
            {
                BuildServerNote(idA, "A", new List<CRDTCharacterServer>
                {
                    new CRDTCharacterServer { IdNote = idA, Payload = payloadA }
                }),
                BuildServerNote(idB, "B", new List<CRDTCharacterServer>
                {
                    new CRDTCharacterServer { IdNote = idB, Payload = payloadB }
                })
            };

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.UpdateBasedOnNoteServer(notes);
            }

            using var verify = NewContext();
            Assert.Equal(2, verify.Notes.Count());
            Assert.Equal(3, verify.CRDTCharacters.Count());
        }

        [Fact]
        public async Task UpdateBasedOnNoteServer_WithMixedNullAndValidCharacters_ProcessesEachNoteIndependently()
        {
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            var payloadB = EncodePayload(idB, ("(1),(b)", 'X'));
            var notes = new List<NoteServer>
            {
                BuildServerNote(idA, "no-chars", characters: null),
                BuildServerNote(idB, "with-chars", new List<CRDTCharacterServer>
                {
                    new CRDTCharacterServer { IdNote = idB, Payload = payloadB }
                })
            };

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.UpdateBasedOnNoteServer(notes);
            }

            using var verify = NewContext();
            Assert.Equal(2, verify.Notes.Count());
            var characters = verify.CRDTCharacters.ToList();
            Assert.Single(characters);
            Assert.Equal(idB, characters[0].IdNote);
        }

        #endregion

        // ==================================================================
        // SaveCRDTChanges
        // ==================================================================
        #region SaveCRDTChanges

        [Fact]
        public async Task SaveCRDTChanges_WithNull_ReturnsWithoutSaving()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            await repository.SaveCRDTChanges(null!);

            using var verify = NewContext();
            Assert.Empty(verify.CRDTCharacters.ToList());
        }

        [Fact]
        public async Task SaveCRDTChanges_WithEmptyList_ReturnsWithoutSaving()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            await repository.SaveCRDTChanges(new List<CRDTCharacterClient>());

            using var verify = NewContext();
            Assert.Empty(verify.CRDTCharacters.ToList());
        }

        [Fact]
        public async Task SaveCRDTChanges_WithNewCharacters_AddsAll()
        {
            using (var context = NewContext())
            {
                context.Notes.Add(BuildNote(_noteId));
                await context.SaveChangesAsync();
            }

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.SaveCRDTChanges(new List<CRDTCharacterClient>
                {
                    BuildChar(_noteId, "(1),(a)", 'H'),
                    BuildChar(_noteId, "(2),(a)", 'i')
                });
            }

            using var verify = NewContext();
            Assert.Equal(2, verify.CRDTCharacters.Count());
        }

        [Fact]
        public async Task SaveCRDTChanges_WithDuplicateInInput_KeepsLastOccurrence()
        {
            using (var context = NewContext())
            {
                context.Notes.Add(BuildNote(_noteId));
                await context.SaveChangesAsync();
            }

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.SaveCRDTChanges(new List<CRDTCharacterClient>
                {
                    BuildChar(_noteId, "(1),(a)", 'A'),
                    BuildChar(_noteId, "(1),(a)", 'Z') // duplicate key, must win
                });
            }

            using var verify = NewContext();
            var persisted = verify.CRDTCharacters.Single();
            Assert.Equal('Z', persisted.Character);
        }

        [Fact]
        public async Task SaveCRDTChanges_WithMixOfNewAndExisting_AddsAndUpdatesAppropriately()
        {
            using (var context = NewContext())
            {
                context.Notes.Add(BuildNote(_noteId));
                context.CRDTCharacters.Add(BuildChar(_noteId, "(1),(a)", 'A', dirty: false));
                await context.SaveChangesAsync();
            }

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                await repository.SaveCRDTChanges(new List<CRDTCharacterClient>
                {
                    BuildChar(_noteId, "(1),(a)", 'B', dirty: true), // existing -> update
                    BuildChar(_noteId, "(2),(a)", 'C', dirty: true)  // new -> add
                });
            }

            using var verify = NewContext();
            var characters = verify.CRDTCharacters.OrderBy(c => c.IdCharacter).ToList();
            Assert.Equal(2, characters.Count);
            Assert.Equal('B', characters[0].Character);
            Assert.Equal('C', characters[1].Character);
        }

        #endregion

        // ==================================================================
        // ClearDirtyFlag
        // ==================================================================
        #region ClearDirtyFlag

        [Fact]
        public async Task ClearDirtyFlag_WithEmptyList_DoesNotThrow()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            var exception = await Record.ExceptionAsync(() =>
                repository.ClearDirtyFlag(new List<NoteClient>()));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ClearDirtyFlag_ClearsFlagsOnNoteAndCharacters()
        {
            using (var context = NewContext())
            {
                context.Notes.Add(BuildNote(_noteId, dirty: true));
                context.CRDTCharacters.AddRange(
                    BuildChar(_noteId, "(1),(a)", 'A', dirty: true),
                    BuildChar(_noteId, "(2),(a)", 'B', dirty: true));
                await context.SaveChangesAsync();
            }

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                var note = BuildNote(_noteId, dirty: true);
                note.CRDTCharacter = new List<CRDTCharacterClient>
                {
                    BuildChar(_noteId, "(1),(a)", 'A', dirty: true),
                    BuildChar(_noteId, "(2),(a)", 'B', dirty: true)
                };

                await repository.ClearDirtyFlag(new List<NoteClient> { note });
            }

            using var verify = NewContext();
            var storedNote = verify.Notes.Single();
            Assert.False(storedNote.DirtyFlagChangesMade);

            var characters = verify.CRDTCharacters.ToList();
            Assert.Equal(2, characters.Count);
            Assert.All(characters, c => Assert.False(c.IsDirtyFlag));
        }

        #endregion

        // ==================================================================
        // UpdateListNotes
        // ==================================================================
        #region UpdateListNotes

        [Fact]
        public void UpdateListNotes_WithNull_ReturnsWithoutSaving()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            repository.UpdateListNotes(null!);

            using var verify = NewContext();
            Assert.Empty(verify.Notes.ToList());
        }

        [Fact]
        public void UpdateListNotes_WithEmptyList_ReturnsWithoutSaving()
        {
            using var context = NewContext();
            var repository = new NoteRepository(context);

            repository.UpdateListNotes(new List<NoteClient>());

            using var verify = NewContext();
            Assert.Empty(verify.Notes.ToList());
        }

        [Fact]
        public void UpdateListNotes_WithOnlyNewNotes_AddsAll()
        {
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                repository.UpdateListNotes(new List<NoteClient>
                {
                    BuildNote(idA, "A"),
                    BuildNote(idB, "B")
                });
            }

            using var verify = NewContext();
            Assert.Equal(2, verify.Notes.Count());
        }

        [Fact]
        public void UpdateListNotes_WithExistingNotes_UpdatesThem()
        {
            using (var context = NewContext())
            {
                context.Notes.Add(BuildNote(_noteId, "old-title"));
                context.SaveChanges();
            }

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                var updated = BuildNote(_noteId, "new-title");
                updated.Version = 42;
                repository.UpdateListNotes(new List<NoteClient> { updated });
            }

            using var verify = NewContext();
            var stored = verify.Notes.Single();
            Assert.Equal("new-title", stored.Title);
            Assert.Equal(42, stored.Version);
        }

        [Fact]
        public void UpdateListNotes_WithMixOfNewAndExisting_AddsAndUpdates()
        {
            var existingId = _noteId;
            var newId = Guid.NewGuid();

            using (var context = NewContext())
            {
                context.Notes.Add(BuildNote(existingId, "existing"));
                context.SaveChanges();
            }

            using (var context = NewContext())
            {
                var repository = new NoteRepository(context);
                repository.UpdateListNotes(new List<NoteClient>
                {
                    BuildNote(existingId, "existing-updated"),
                    BuildNote(newId, "brand-new")
                });
            }

            using var verify = NewContext();
            var notes = verify.Notes.OrderBy(n => n.Title).ToList();
            Assert.Equal(2, notes.Count);
            Assert.Equal("brand-new", notes[0].Title);
            Assert.Equal("existing-updated", notes[1].Title);
        }

        #endregion
    }
}
