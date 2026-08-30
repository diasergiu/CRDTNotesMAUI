using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services.ServerRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MAUIClientUI.Test.Offline
{
    /// <summary>
    /// Offline queueing, retry, and durability tests.
    ///
    /// These exercise the real client SQLite database through <see cref="DbContextClient"/>,
    /// so the durability assertions are made against genuine persisted state rather than an
    /// in-memory stand-in. Each test uses a unique database instance id and deletes the
    /// file afterwards.
    ///
    /// Test names map onto Documentation/TestPlan/TestPlan.md (TC-28 .. TC-34).
    /// </summary>
    public class OfflineSyncAndDurabilityTests : IDisposable
    {
        private readonly string _instanceId;
        private readonly Guid _noteId = Guid.NewGuid();

        public OfflineSyncAndDurabilityTests()
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
                // Best-effort cleanup; a leaked temp database must not fail the suite.
            }
        }

        private CRDTCharacterClient DirtyChar(string id, char value) => new CRDTCharacterClient
        {
            IdCharacter = id,
            IdNote = _noteId,
            Character = value,
           
            Tombstone = false,
            ClockDateTime = DateTime.UtcNow,
            IsDirtyFlag = true
        };

        private async Task SeedNoteAsync(DbContextClient context)
        {
            context.Notes.Add(new NoteClient
            {
                IdNote = _noteId,
                Title = "Offline note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1
            });
            await context.SaveChangesAsync();
        }

        // -----------------------------------------------------------------
        // TC-28 Offline edits are queued locally
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_28_EditsMadeWhileOfflineArePersistedAsDirty()
        {
            using var context = NewContext();
            await SeedNoteAsync(context);
            var repository = new NoteRepository(context);

            await repository.SaveCRDTChanges(new List<CRDTCharacterClient>
            {
                DirtyChar("(1),(a)", 'O'),
                DirtyChar("(2),(a)", 'F'),
                DirtyChar("(3),(a)", 'F')
            });

            var queued = await repository.GetAllCRDTCharacters();

            Assert.Equal(3, queued.Count);
            Assert.All(queued, c => Assert.True(c.IsDirtyFlag));
        }

        [Fact]
        public async Task TC_28_OfflineEditsSucceedWithoutAnyServerInteraction()
        {
            using var context = NewContext();
            await SeedNoteAsync(context);
            var repository = new NoteRepository(context);

            // No HTTP client is involved at all: local editing must never depend on the
            // network (FR-07).
            var exception = await Record.ExceptionAsync(() =>
                repository.SaveCRDTChanges(new List<CRDTCharacterClient> { DirtyChar("(1),(a)", 'X') }));

            Assert.Null(exception);
        }

        // -----------------------------------------------------------------
        // TC-29 Dirty flags clear after a successful sync
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_29_ClearingDirtyFlagRemovesRecordsFromTheSyncQueue()
        {
            using var context = NewContext();
            await SeedNoteAsync(context);
            var repository = new NoteRepository(context);

            var queuedOperations = new List<CRDTCharacterClient>
            {
                DirtyChar("(1),(a)", 'A'),
                DirtyChar("(2),(a)", 'B')
            };
            await repository.SaveCRDTChanges(queuedOperations);
            Assert.Equal(2, (await repository.GetAllCRDTCharacters()).Count);

            // Simulate the server acknowledging the batch.
            // Create a NoteClient with the CRDT characters to pass to ClearDirtyFlag
            var noteToClear = new NoteClient
            {
                IdNote = _noteId,
                Title = "Offline note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1,
                DirtyFlagChangesMade = true,
                CRDTCharacter = queuedOperations
            };
            await repository.ClearDirtyFlag(new List<NoteClient> { noteToClear });

            using var verifyContext = NewContext();
            var verifyRepository = new NoteRepository(verifyContext);
            var stillQueued = await verifyRepository.GetAllCRDTCharacters();

            Assert.Empty(stillQueued);

            // The operations themselves must remain; only the flag changes.
            Assert.Equal(2, verifyContext.CRDTCharacters.Count());
        }

        // -----------------------------------------------------------------
        // TC-30 / TC-31 Retry after a transient network failure
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_30_ConnectionFailureIsClassifiedAsRecoverableConnectionError()
        {
            var handler = new SequencedHandler(
                new Func<HttpResponseMessage>[]
                {
                    () => throw new HttpRequestException("Simulated network is unreachable")
                });

            using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

            var result = await ExceptionHandlingHelper.ExecuteAsync(
                () => client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/api/notes/SendCRDTChangestoServer")),
                "SendCRDTChangestoServer");

            Assert.False(result.IsSuccess);
            Assert.Equal(ApiErrorType.ConnectionError, result.ErrorType);
        }

        [Fact]
        public async Task TC_30_DirtyRecordsSurviveAFailedSyncAttemptAndRemainQueued()
        {
            using var context = NewContext();
            await SeedNoteAsync(context);
            var repository = new NoteRepository(context);

            var queuedOperations = new List<CRDTCharacterClient>
            {
                DirtyChar("(1),(a)", 'R'),
                DirtyChar("(2),(a)", 'T')
            };
            await repository.SaveCRDTChanges(queuedOperations);

            // A failing transport means the acknowledgement never arrives, so
            // ClearDirtyFlag is never invoked (see NoteServices.SendCRDTChangestoServer,
            // which only clears on IsSuccess).
            var handler = new SequencedHandler(
                new Func<HttpResponseMessage>[]
                {
                    () => throw new HttpRequestException("Simulated connection reset")
                });
            using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

            var syncResult = await ExceptionHandlingHelper.ExecuteAsync(
                () => client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/api/notes/SendCRDTChangestoServer")),
                "SendCRDTChangestoServer");

            Assert.False(syncResult.IsSuccess);

            // Nothing was lost; the batch is still pending for the next sync cycle.
            var stillQueued = await repository.GetAllCRDTCharacters();
            Assert.Equal(2, stillQueued.Count);
            Assert.All(stillQueued, c => Assert.True(c.IsDirtyFlag));
        }

        [Fact]
        public async Task TC_31_RetryAfterTransientFailuresEventuallySucceedsWithNoOperationLoss()
        {
            using var context = NewContext();
            await SeedNoteAsync(context);
            var repository = new NoteRepository(context);

            var queuedOperations = new List<CRDTCharacterClient>
            {
                DirtyChar("(1),(a)", 'Y'),
                DirtyChar("(2),(a)", 'Z')
            };
            await repository.SaveCRDTChanges(queuedOperations);

            // Fail twice, then succeed.
            var handler = new SequencedHandler(new Func<HttpResponseMessage>[]
            {
                () => throw new HttpRequestException("Attempt 1 failed"),
                () => throw new HttpRequestException("Attempt 2 failed"),
                () => new HttpResponseMessage(HttpStatusCode.OK)
            });
            using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

            ApiResult? result = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                result = await ExceptionHandlingHelper.ExecuteAsync(
                    () => client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/api/notes/SendCRDTChangestoServer")),
                    "SendCRDTChangestoServer");

                if (result.IsSuccess)
                {
                    // Create a NoteClient with the CRDT characters to pass to ClearDirtyFlag
                    var noteToClear = new NoteClient
                    {
                        IdNote = _noteId,
                        Title = "Offline note",
                        CreationDate = DateTime.UtcNow,
                        LastUpdate = DateTime.UtcNow,
                        Version = 1,
                        DirtyFlagChangesMade = true,
                        CRDTCharacter = queuedOperations
                    };
                    await repository.ClearDirtyFlag(new List<NoteClient> { noteToClear });
                    break;
                }

                // Between attempts the queue must be intact.
                Assert.Equal(2, (await repository.GetAllCRDTCharacters()).Count);
            }

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(3, handler.CallCount);

            using var verifyContext = NewContext();
            Assert.Empty(await new NoteRepository(verifyContext).GetAllCRDTCharacters());
            Assert.Equal(2, verifyContext.CRDTCharacters.Count());
        }

        [Fact]
        public async Task TC_31_ServerErrorResponseIsNotTreatedAsASuccessfulSync()
        {
            var handler = new SequencedHandler(new Func<HttpResponseMessage>[]
            {
                () => new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("boom")
                }
            });
            using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

            var result = await ExceptionHandlingHelper.ExecuteAsync(
                () => client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/api/notes/SendCRDTChangestoServer")),
                "SendCRDTChangestoServer");

            // Critical: if this were reported as success, NoteServices would clear the
            // dirty flags and the queued operations would be lost permanently.
            Assert.False(result.IsSuccess);
            Assert.Equal(ApiErrorType.ServerError, result.ErrorType);
        }

        // -----------------------------------------------------------------
        // TC-32 Durability across a simulated application restart
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_32_QueuedOperationsSurviveApplicationRestart()
        {
            using (var context = NewContext())
            {
                await SeedNoteAsync(context);
                var repository = new NoteRepository(context);
                await repository.SaveCRDTChanges(new List<CRDTCharacterClient>
                {
                    DirtyChar("(1),(a)", 'P'),
                    DirtyChar("(2),(a)", 'E'),
                    DirtyChar("(3),(a)", 'R')
                });
            }

            // The context (and therefore the app) is gone. Open the local database again
            // exactly as a cold start would.
            using var restarted = NewContext();
            var restartedRepository = new NoteRepository(restarted);

            var queued = await restartedRepository.GetAllCRDTCharacters();

            Assert.Equal(3, queued.Count);
            Assert.All(queued, c => Assert.True(c.IsDirtyFlag));
            Assert.Equal("PER", new string(queued
                .OrderBy(c => c.IdCharacter, StringComparer.Ordinal)
                .Select(c => c.Character)
                .ToArray()));
        }

        [Fact]
        public async Task TC_32_ClearedFlagsAlsoSurviveApplicationRestart()
        {
            var operations = new List<CRDTCharacterClient>
            {
                DirtyChar("(1),(a)", 'S'),
                DirtyChar("(2),(a)", 'Y')
            };

            using (var context = NewContext())
            {
                await SeedNoteAsync(context);
                var repository = new NoteRepository(context);
                await repository.SaveCRDTChanges(operations);

                // Create a NoteClient with the CRDT characters to pass to ClearDirtyFlag
                var noteToClear = new NoteClient
                {
                    IdNote = _noteId,
                    Title = "Offline note",
                    CreationDate = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    Version = 1,
                    DirtyFlagChangesMade = true,
                    CRDTCharacter = operations
                };
                await repository.ClearDirtyFlag(new List<NoteClient> { noteToClear });
            }

            using var restarted = NewContext();
            Assert.Empty(await new NoteRepository(restarted).GetAllCRDTCharacters());
            Assert.Equal(2, restarted.CRDTCharacters.Count());
        }

        // -----------------------------------------------------------------
        // TC-34 Only dirty records are transmitted
        // -----------------------------------------------------------------

        [Fact]
        public async Task TC_34_SyncQueueContainsOnlyDirtyRecords()
        {
            using var context = NewContext();
            await SeedNoteAsync(context);
            var repository = new NoteRepository(context);

            var alreadySynced = new List<CRDTCharacterClient>
            {
                DirtyChar("(1),(a)", 'C'),
                DirtyChar("(2),(a)", 'L')
            };
            await repository.SaveCRDTChanges(alreadySynced);

            // Create a NoteClient with the CRDT characters to pass to ClearDirtyFlag
            var noteToClear = new NoteClient
            {
                IdNote = _noteId,
                Title = "Offline note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1,
                DirtyFlagChangesMade = true,
                CRDTCharacter = alreadySynced
            };
            await repository.ClearDirtyFlag(new List<NoteClient> { noteToClear });

            // A new offline edit arrives after the previous batch was acknowledged.
            await repository.SaveCRDTChanges(new List<CRDTCharacterClient>
            {
                DirtyChar("(3),(a)", 'N')
            });

            var queued = await repository.GetAllCRDTCharacters();

            Assert.Single(queued);
            Assert.Equal('N', queued[0].Character);
            Assert.Equal(3, context.CRDTCharacters.Count());
        }

        /// <summary>
        /// Test transport that plays a scripted sequence of outcomes, so that transient
        /// failure followed by recovery can be simulated deterministically.
        /// </summary>
        private sealed class SequencedHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage>[] _steps;
            private int _index;

            public SequencedHandler(Func<HttpResponseMessage>[] steps) => _steps = steps;

            public int CallCount => _index;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var step = _steps[Math.Min(_index, _steps.Length - 1)];
                _index++;
                try
                {
                    return Task.FromResult(step());
                }
                catch (Exception ex)
                {
                    return Task.FromException<HttpResponseMessage>(ex);
                }
            }
        }
    }
}
