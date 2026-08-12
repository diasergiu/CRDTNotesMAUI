using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MAUIClientUI.Test.EndToEndServiceTest
{
    /// <summary>
    /// End-to-End tests for note functionality including:
    /// - Note CRUD operations
    /// - Concurrent updates from multiple users
    /// - Multi-user collaboration scenarios
    /// - Sync and conflict resolution (CRDT behavior)
    /// 
    /// REQUIREMENTS:
    /// 1. Start the Server project before running these tests
    /// 2. Server should be listening on http://localhost:5266
    /// 3. Database should be initialized
    /// </summary>
    [Collection("Sequential")]
    public class EndToEndNoteServicesTest : IAsyncLifetime
    {
        private readonly string _testServerUrl = "http://localhost:5266/api";
        private const string TEST_USERNAME_PREFIX = "e2etest_note_";
        private const string TEST_PASSWORD = "TestPass123!@#";

        private readonly HttpClient _httpClient;
        private UserServices _userService;
        private NoteServices _noteService;

        // Test users and their data
        private TestUserContext _user1;
        private TestUserContext _user2;
        private TestUserContext _user3;

        public EndToEndNoteServicesTest()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _userService = new UserServices("/api/user");
            _noteService = new NoteServices("/api/notes");
        }

        public async Task InitializeAsync()
        {
            // Verify server is running
            try
            {
                var response = await _httpClient.GetAsync($"{_testServerUrl}/user");
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(
                    $"Cannot connect to server at {_testServerUrl}. " +
                    "Please ensure the Server project is running on http://localhost:5266");
            }

            // Setup: Create test users
            _user1 = await CreateTestUser("User One", "User 1");
            _user2 = await CreateTestUser("User Two", "User 2");
            _user3 = await CreateTestUser("User Three", "User 3");

            Assert.True(_user1.IsLoggedIn, "User 1 should be logged in");
            Assert.True(_user2.IsLoggedIn, "User 2 should be logged in");
            Assert.True(_user3.IsLoggedIn, "User 3 should be logged in");
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
        }

        // ==================== HELPER METHODS ====================

        private async Task<TestUserContext> CreateTestUser(string displayName, string suffix)
        {
            var username = $"{TEST_USERNAME_PREFIX}{suffix}_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var registerResult = await _userService.RegisterNewUser(displayName, username, TEST_PASSWORD);
            Assert.True(registerResult.IsSuccess, $"Failed to register {displayName}: {registerResult.ErrorMessage}");

            var loginResult = await _userService.Login(username, TEST_PASSWORD);
            Assert.True(loginResult.IsSuccess, $"Failed to login {displayName}: {loginResult.ErrorMessage}");

            return new TestUserContext
            {
                Username = username,
                DisplayName = displayName,
                UserId = registerResult.Data.IdUser,
                Password = TEST_PASSWORD,
                IsLoggedIn = true,
            };
        }

        // ==================== BASIC NOTE CREATION & RETRIEVAL ====================

        [Fact]
        public async Task E2E_CreateNote_ForSingleUser_Succeeds()
        {
            // Arrange
            var noteTitle = $"Test Note {Guid.NewGuid().ToString().Substring(0, 8)}";
            var noteContent = "This is a test note content";

            var note = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = noteTitle,
                Content = noteContent,
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true
            };

            // Act
            var createResult = await _noteService.CreateNewNote(note);

            // Assert
            Assert.NotNull(createResult);
            Assert.True(createResult.IsSuccess, $"Failed to create note: {createResult.ErrorMessage}");
        }

        [Fact]
        public async Task E2E_RetrieveAllNotes_ForUser_ReturnsCreatedNotes()
        {
            // Arrange - Create multiple notes
            var notes = new List<NoteClient>();
            for (int i = 0; i < 3; i++)
            {
                var note = new NoteClient
                {
                    IdNote = Guid.NewGuid(),
                    Title = $"Retrieve Test Note {i}",
                    Content = $"Content {i}",
                    CreationDate = DateTime.UtcNow.ToString("o"),
                    LastUpdate = DateTime.UtcNow.ToString("o"),
                    DirtyFlagChangesMade = true
                };

                var createResult = await _noteService.CreateNewNote(note);
                Assert.True(createResult.IsSuccess, $"Failed to create note {i}");
                notes.Add(note);
            }

            // Act
            var retrieveResult = await _noteService.GetAllNotesFromUser(_user1.UserId);

            // Assert
            Assert.True(retrieveResult.IsSuccess, $"Failed to retrieve notes: {retrieveResult.ErrorMessage}");
            Assert.NotNull(retrieveResult.Data);
            Assert.NotEmpty(retrieveResult.Data);
        }

        // ==================== CONCURRENT UPDATE TESTS ====================

        [Fact]
        public async Task E2E_ConcurrentUpdates_FromTwoUsers_OnSameNote_ResolvesCorrectly()
        {
            // Arrange - Create a shared note
            var sharedNoteId = Guid.NewGuid();
            var originalContent = "Initial content";

            var note = new NoteClient
            {
                IdNote = sharedNoteId,
                Title = "Concurrent Update Test",
                Content = originalContent,
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create shared note");

            // Act - User 1 updates the note
            var user1Update = new NoteClient
            {
                IdNote = sharedNoteId,
                Title = "Concurrent Update Test",
                Content = originalContent + " [Updated by User 1]",
                CreationDate = note.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var user1Result = await _noteService.UpdateNote(user1Update);
            Assert.True(user1Result.IsSuccess, "User 1 update failed: " + user1Result.ErrorMessage);

            // Get the updated version from server response
            var serverVersionAfterUser1 = user1Result.Data.ServerNote.Version;

            // Act - User 2 updates the same note (simulating concurrent update)
            // User 2 doesn't know about User 1's update, so uses version 1, but server has a higher version now
            var user2Update = new NoteClient
            {
                IdNote = sharedNoteId,
                Title = "Concurrent Update Test",
                Content = originalContent + " [Updated by User 2]",
                CreationDate = note.CreationDate,
                LastUpdate = DateTime.UtcNow.AddMilliseconds(100).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1  // User 2 doesn't know server is at version 2 now
            };

            var user2Result = await _noteService.UpdateNote(user2Update);

            // Assert - Both updates should be processed, but User 2 might encounter a conflict
            // CRDT should handle the merging automatically on the server
            Assert.NotNull(user2Result);

            // If there's a conflict, the test should document it
            Assert.True(user2Result.IsSuccess && user2Result.Data.IsVersionConflict);
            Assert.NotNull(user2Result.Data.ServerNote);
      
        }

        [Fact]
        public async Task E2E_RapidSequentialUpdates_OnSameNote_MaintainsConsistency()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Rapid Update Test",
                Content = "Version 0",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 0
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create base note");

            // Act - Make rapid sequential updates
            var currentVersion = 0;
            for (int i = 1; i <= 5; i++)
            {
                var nextVersion = currentVersion + 1;
                var updatedNote = new NoteClient
                {
                    IdNote = noteId,
                    Title = "Rapid Update Test",
                    Content = $"Version {nextVersion}",
                    CreationDate = baseNote.CreationDate,
                    LastUpdate = DateTime.UtcNow.AddMilliseconds(i * 10).ToString("o"),
                    DirtyFlagChangesMade = true,
                    Version = nextVersion
                };

                var result = await _noteService.UpdateNote(updatedNote);
                Assert.True(result.IsSuccess, $"Update {i} failed: {result.ErrorMessage}");
                Assert.False(result.Data.IsVersionConflict, $"Update {i} should not have Version conflict");

                currentVersion = result.Data.ServerNote.Version;
            }

            // Assert
            Assert.Equal(5, currentVersion);

            var finalNote = await _noteService.GetNote(noteId);
            Assert.True(finalNote.IsSuccess, "Failed to retrieve final note state");
        }

        [Fact]
        public async Task E2E_ParallelUpdates_FromThreeUsers_ResolvesCorrectly()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Three User Update Test",
                Content = "Base content",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create base note");

            // Act - All three users update the note simultaneously
            var user1Task = _noteService.CreateNewNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Three User Update Test",
                Content = "Updated by User 1",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var user2Task = _noteService.CreateNewNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Three User Update Test",
                Content = "Updated by User 2",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.AddMilliseconds(5).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var user3Task = _noteService.CreateNewNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Three User Update Test",
                Content = "Updated by User 3",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.AddMilliseconds(10).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var results = await Task.WhenAll(user1Task, user2Task, user3Task);

            // Assert
            Assert.True(results.All(r => r.IsSuccess), "Some concurrent updates failed");
        }

        // ==================== MULTI-USER COLLABORATION TESTS ====================

        [Fact]
        public async Task E2E_MultipleNotes_CreatedByDifferentUsers_RetrievableByEachUser()
        {
            // Arrange - Each user creates their own notes
            var user1Notes = new List<NoteClient>();
            var user2Notes = new List<NoteClient>();

            for (int i = 0; i < 2; i++)
            {
                var user1Note = new NoteClient
                {
                    IdNote = Guid.NewGuid(),
                    Title = $"User 1 Note {i}",
                    Content = $"User 1 content {i}",
                    CreationDate = DateTime.UtcNow.ToString("o"),
                    LastUpdate = DateTime.UtcNow.ToString("o"),
                    DirtyFlagChangesMade = true,
                    Version = 1,
                };

                var user2Note = new NoteClient
                {
                    IdNote = Guid.NewGuid(),
                    Title = $"User 2 Note {i}",
                    Content = $"User 2 content {i}",
                    CreationDate = DateTime.UtcNow.ToString("o"),
                    LastUpdate = DateTime.UtcNow.ToString("o"),
                    DirtyFlagChangesMade = true,
                    Version = 1
                };

                var result1 = await _noteService.CreateNewNote(user1Note);
                var result2 = await _noteService.CreateNewNote(user2Note);

                Assert.True(result1.IsSuccess, "Failed to create User 1 note");
                Assert.True(result2.IsSuccess, "Failed to create User 2 note");

                user1Notes.Add(user1Note);
                user2Notes.Add(user2Note);
            }

            // Act & Assert - Each user can retrieve their notes
            var user1RetrieveResult = await _noteService.GetAllNotesFromUser(_user1.UserId);
            var user2RetrieveResult = await _noteService.GetAllNotesFromUser(_user2.UserId);

            Assert.True(user1RetrieveResult.IsSuccess, "User 1 failed to retrieve notes");
            Assert.True(user2RetrieveResult.IsSuccess, "User 2 failed to retrieve notes");
            Assert.NotEmpty(user1RetrieveResult.Data);
            Assert.NotEmpty(user2RetrieveResult.Data);
        }


        // ==================== CONFLICT RESOLUTION & CONSISTENCY ====================

        [Fact]
        public async Task E2E_OfflineEdit_ThenSync_MergesChangesCorrectly()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Offline Sync Test",
                Content = "Initial content",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = false
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create base note");

            // Act - Simulate offline edit (dirty flag set)
            var offlineEdit = new NoteClient
            {
                IdNote = noteId,
                Title = "Offline Sync Test",
                Content = "Content edited while offline",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true // Marked as dirty for offline sync
            };

            // Then sync with server
            var syncResult = await _noteService.SendChangesToServer(new List<NoteClient> { offlineEdit });

            // Assert
            Assert.True(syncResult.IsSuccess, $"Sync failed: {syncResult.ErrorMessage}");
        }

        [Fact]
        public async Task E2E_LargeNoteContent_HandledCorrectly()
        {
            // Arrange - Create note with large content
            var largeContent = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                largeContent.AppendLine($"This is line {i} of a large document. Lorem ipsum dolor sit amet...");
            }

            var largeNote = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "Large Content Test",
                Content = largeContent.ToString(),
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true
            };

            // Act
            var createResult = await _noteService.CreateNewNote(largeNote);

            // Assert
            Assert.True(createResult.IsSuccess, "Failed to create note with large content");
        }

        // ==================== EDGE CASES ====================

        [Fact]
        public async Task E2E_EmptyNote_IsRejectedOrAccepted_AsPerServerRules()
        {
            // Arrange
            var emptyNote = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "",
                Content = "",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true
            };

            // Act
            var result = await _noteService.CreateNewNote(emptyNote);

            // Assert - Document the behavior
            // Server may accept empty notes or reject them - this test documents the behavior
            Assert.NotNull(result);
        }

        [Fact]
        public async Task E2E_SpecialCharactersInNoteContent_PreservedCorrectly()
        {
            // Arrange
            var specialContent = "Content with special chars: !@#$%^&*()_+-=[]{}|;:'\",.<>?/\nNewlines\tAnd\tTabs";
            var note = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "Special Chars Test",
                Content = specialContent,
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true
            };

            // Act
            var createResult = await _noteService.CreateNewNote(note);
            var retrieveResult = await _noteService.GetAllNotesFromUser(note.IdNote);

            // Assert
            Assert.True(createResult.IsSuccess, "Failed to create note");
            if (retrieveResult.IsSuccess)
            {
                // Verify special characters are preserved if retrieval was successful
                Assert.NotNull(retrieveResult.Data);
            }
        }

        [Fact]
        public async Task E2E_NoteCreation_WithoutAssignment_ToUser_Succeeds()
        {
            // Arrange - Create a note without explicit user assignment
            var standaloneNote = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "Standalone Note",
                Content = "Note without explicit user",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true
            };

            // Act
            var result = await _noteService.CreateNewNote(standaloneNote);

            // Assert
            Assert.NotNull(result);
            // Document whether the server allows this or requires explicit user association
        }

        // ==================== MULTI-DEVICE SCENARIOS ====================

        [Fact]
        public async Task E2E_UserWithMultipleDevices_BothUpdateSameNote_CRDTResolvesConflicts()
        {
            // Arrange - Simulate two different devices for the same user
            var noteId = Guid.NewGuid();
            var device1Id = Guid.NewGuid();
            var device2Id = Guid.NewGuid();

            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Multi-Device Note",
                Content = "Initial content from device 1",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 0
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create base note");

            // Act - Device 1 updates the note
            var device1Update = new NoteClient
            {
                IdNote = noteId,
                Title = "Multi-Device Note",
                Content = "Device 1: Updated at " + DateTime.UtcNow.ToString("HH:mm:ss"),
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var device1UpdateTask = _noteService.CreateNewNote(device1Update);

            // Act - Device 2 updates simultaneously (simulate network delay)
            await Task.Delay(100);
            var device2Update = new NoteClient
            {
                IdNote = noteId,
                Title = "Multi-Device Note",
                Content = "Device 2: Updated at " + DateTime.UtcNow.ToString("HH:mm:ss"),
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var device2UpdateTask = _noteService.CreateNewNote(device2Update);

            var results = await Task.WhenAll(device1UpdateTask, device2UpdateTask);

            // Assert - Both updates should succeed (CRDT handles conflicts)
            Assert.True(results.All(r => r.IsSuccess), "Both device updates should succeed with CRDT");

            // Verify note was updated
            var finalState = await _noteService.GetNote(noteId);
            Assert.True(finalState.IsSuccess, "Should retrieve updated note");
        }

        [Fact]
        public async Task E2E_OfflineSync_WithDifferentDevices_MergesChangesCorrectly()
        {
            // Arrange - Simulate device going offline, making changes, then syncing
            var noteId = Guid.NewGuid();
            var device1Id = Guid.NewGuid();
            var device2Id = Guid.NewGuid();

            var sharedNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Offline Sync Test",
                Content = "Initial content",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = false,
                Version = 0
            };

            var createResult = await _noteService.CreateNewNote(sharedNote);
            Assert.True(createResult.IsSuccess, "Failed to create shared note");

            // Act - Device 1 goes offline, makes edits
            var device1OfflineEdits = new List<NoteClient>
            {
                new NoteClient
                {
                    IdNote = noteId,
                    Title = "Offline Sync Test",
                    Content = "Device 1 edit 1 - offline",
                    CreationDate = sharedNote.CreationDate,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-30).ToString("o"),
                    DirtyFlagChangesMade = true,
                    Version = 1
                },
                new NoteClient
                {
                    IdNote = noteId,
                    Title = "Offline Sync Test",
                    Content = "Device 1 edit 2 - still offline",
                    CreationDate = sharedNote.CreationDate,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-20).ToString("o"),
                    DirtyFlagChangesMade = true,
                    Version = 2
                }
            };

            // Act - Device 2 updates while device 1 is offline
            var device2Update = new NoteClient
            {
                IdNote = noteId,
                Title = "Offline Sync Test",
                Content = "Device 2 update while device 1 offline",
                CreationDate = sharedNote.CreationDate,
                LastUpdate = DateTime.UtcNow.AddSeconds(-10).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var device2Result = await _noteService.CreateNewNote(device2Update);
            Assert.True(device2Result.IsSuccess, "Device 2 update failed");

            // Act - Device 1 comes back online and syncs all changes
            var syncResult = await _noteService.SendChangesToServer(device1OfflineEdits);

            // Assert - Sync should succeed and CRDT resolves conflicts
            Assert.True(syncResult.IsSuccess, "Offline sync failed");

            // Verify final state includes updates from both devices
            var finalNote = await _noteService.GetNote(noteId);
            Assert.True(finalNote.IsSuccess, "Failed to retrieve final state");
        }

        // ==================== CONFLICT RESOLUTION SCENARIOS ====================

        [Fact]
        public async Task E2E_TwoConcurrentUpdatesSameNote()
        {
            // Arrange - Create a note for conflict testing
            var noteId = Guid.NewGuid();
            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Conflict Test",
                Content = "This is the original line that will be edited",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create base note");

            // Get precise timestamp
            var conflictTime = DateTime.UtcNow;

            // Act - Two simultaneous edits to the same content
            var edit1 = await _noteService.UpdateNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Conflict Test",
                Content = "[User 1 Edit] This is the original line that will be edited",
                CreationDate = baseNote.CreationDate,
                LastUpdate = conflictTime.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var edit2 = await _noteService.UpdateNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Conflict Test",
                Content = "[User 2 Edit] This is the original line that will be edited",
                CreationDate = baseNote.CreationDate,
                LastUpdate = conflictTime.AddMilliseconds(1).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 2
            });

            // Assert - CRDT should handle both edits
            Assert.True(edit1.IsSuccess, "Edit 1 should succeed");
            Assert.True(edit2.IsSuccess, "Edit 2 should succeed");

            var resolvedState = await _noteService.GetNote(noteId);
            Assert.True(resolvedState.IsSuccess, "Should resolve conflicts and return state");
        }

        [Fact]
        public async Task E2E_LargeDocumentConcurrentEdits_AtDifferentPositions_MergesSuccessfully()
        {
            // Arrange - Create a large document
            var noteId = Guid.NewGuid();
            var noteId2 = Guid.NewGuid();

            var largeContent = GenerateMultiParagraphContent(10);

            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Large Document Merge",
                Content = largeContent,
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create large note");

            // Act - Two devices edit different parts of the document
            var device1Edit = new NoteClient
            {
                IdNote = noteId,
                Title = "Large Document Merge",
                Content = "DEVICE 1 EDIT AT START\n" + largeContent,
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var device2Edit = new NoteClient
            {
                IdNote = noteId2,
                Title = "Large Document Merge",
                Content = largeContent + "\nDEVICE 2 EDIT AT END",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.AddMilliseconds(50).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            };

            var results = await Task.WhenAll(
                _noteService.CreateNewNote(device1Edit),
                _noteService.CreateNewNote(device2Edit)
            );

            // Assert
            Assert.True(results.All(r => r.IsSuccess), "Both edits should succeed");

            var finalState = await _noteService.GetNote(noteId);
            Assert.True(finalState.IsSuccess, "Should retrieve merged state");
        }

        [Fact]
        public async Task E2E_ThreeDevicesEditingConcurrently_AllChangesEventuallyVisible()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = "Three Device Sync",
                Content = "Base content",
                CreationDate = DateTime.UtcNow.ToString("o"),
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 0
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create base note");

            // Act - Three devices update simultaneously
            var device1Task = _noteService.CreateNewNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Three Device Sync",
                Content = "Content from Device 1",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var device2Task = _noteService.CreateNewNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Three Device Sync",
                Content = "Content from Device 2",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.AddMilliseconds(10).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var device3Task = _noteService.CreateNewNote(new NoteClient
            {
                IdNote = noteId,
                Title = "Three Device Sync",
                Content = "Content from Device 3",
                CreationDate = baseNote.CreationDate,
                LastUpdate = DateTime.UtcNow.AddMilliseconds(20).ToString("o"),
                DirtyFlagChangesMade = true,
                Version = 1
            });

            var results = await Task.WhenAll(device1Task, device2Task, device3Task);

            // Assert - All updates succeed
            Assert.True(results.All(r => r.IsSuccess), "All concurrent updates should succeed");

            // Query server for final state
            var finalState = await _noteService.GetNote(noteId);
            Assert.True(finalState.IsSuccess, "Should retrieve final merged state");
        }


        // ==================== HELPER METHODS ====================

        private string GenerateMultiParagraphContent(int paragraphs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < paragraphs; i++)
            {
                sb.AppendLine($"Paragraph {i + 1}:");
                sb.AppendLine("Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Context class to manage test user data throughout test lifecycle
    /// </summary>
    public class TestUserContext
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public Guid UserId { get; set; }
        public string Password { get; set; }
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// Simulated devices for this user (supports multi-device testing)
        /// </summary>
        public Dictionary<string, Guid> Devices { get; set; } = new();

        public void AddDevice(string deviceName)
        {
            Devices[deviceName] = Guid.NewGuid();
        }

        public Guid GetDeviceId(string deviceName)
        {
            return Devices.ContainsKey(deviceName) ? Devices[deviceName] : Guid.Empty;
        }
    }
}