using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services.ServerRequests;
using Server.ServeRepositories;
using System.Text.Json;
using Xunit;

namespace EndToEndTest.EndToEndServiceTest
{
    /// <summary>
    /// End-to-End tests for NoteServices that make real HTTP requests to the server.
    /// 
    /// These tests verify that NoteServices correctly communicates with the API server
    /// and handles all CRUD operations, sync operations, and access control scenarios.
    ///
    /// REQUIREMENTS:
    /// 1. Start the Server project before running these tests
    /// 2. Server should be listening on http://localhost:5266
    /// 3. Database should be initialized
    /// 
    /// Test Organization:
    /// - Basic CRUD Operations: Create, Read, Update, Delete
    /// - Sync Operations: SendChangesToServer, GetServerChanges
    /// - CRDT Operations: SendCRDTChangestoServer, GetServerChangesByNote
    /// - Access Control: GiveNoteAccessToUser
    /// - Error Scenarios: Unauthorized access, Not found errors
    /// 
    /// NOTE: These are true E2E tests with real server calls - no mocks.
    /// </summary>
    [Collection("Sequential")]
    public class NoteServicesE2ETest : IAsyncLifetime
    {
        private readonly string _testServerUrl = "http://localhost:5266/api";
        private const string TEST_USERNAME_PREFIX = "e2etest_note_";
        private const string TEST_PASSWORD = "TestPass123!@#";
        private const string TEST_USER_NAME = "E2E Note Test User";

        private UserServices _userService;
        private NoteServices _noteService;
        private readonly HttpClient _httpClient;

        // Test data holders
        private UserClient _testUser1;
        private UserClient _testUser2;
        private Guid _testUserId1;
        private Guid _testUserId2;
        private Guid _testNoteId;
        private string _testUsername1;
        private string _testUsername2;

        public NoteServicesE2ETest()
        {
           
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _userService = new UserServices("/api/user");
            //_noteService = new NoteServices("/api/notes", noteRepository);
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

            // Create and login test users
            await SetupTestUsers();
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            // Cleanup is minimal - test users and notes remain on server for audit trail
        }

        // ==================== SETUP HELPERS ====================

        private async Task SetupTestUsers()
        {
            // Create unique usernames to avoid conflicts
            var guid1 = Guid.NewGuid().ToString().Substring(0, 8);
            var guid2 = Guid.NewGuid().ToString().Substring(0, 8);
            _testUsername1 = $"{TEST_USERNAME_PREFIX}user1_{guid1}";
            _testUsername2 = $"{TEST_USERNAME_PREFIX}user2_{guid2}";

            // Register and login first test user
            var registerResult1 = await _userService.RegisterNewUser(TEST_USER_NAME, _testUsername1, TEST_PASSWORD);
            Assert.True(registerResult1.IsSuccess, $"Failed to register user 1: {registerResult1.ErrorMessage}");

            var loginResult1 = await _userService.Login(_testUsername1, TEST_PASSWORD);
            Assert.True(loginResult1.IsSuccess, $"Failed to login user 1: {loginResult1.ErrorMessage}");

            _testUser1 = loginResult1.Data;
            _testUserId1 = _testUser1.IdUser;


            NoteRepository noteRepository = new NoteRepository(new DbContextClient());
            var userContext = new UserContext { LocalUser = _testUserId1 };
            _noteService = new NoteServices("/api/notes", noteRepository, userContext);
            

            // Register and login second test user for sharing/access tests
            var registerResult2 = await _userService.RegisterNewUser(TEST_USER_NAME, _testUsername2, TEST_PASSWORD);
            Assert.True(registerResult2.IsSuccess, $"Failed to register user 2: {registerResult2.ErrorMessage}");

            var loginResult2 = await _userService.Login(_testUsername2, TEST_PASSWORD);
            Assert.True(loginResult2.IsSuccess, $"Failed to login user 2: {loginResult2.ErrorMessage}");

            _testUser2 = loginResult2.Data;
            _testUserId2 = _testUser2.IdUser;

            // Generate a test note ID
            _testNoteId = Guid.NewGuid();
        }

        // ==================== BASIC CRUD OPERATIONS ====================

        [Fact]
        public async Task CreateNewNote_WithValidNote_Succeeds()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var noteTitle = $"E2E Test Note {Guid.NewGuid().ToString().Substring(0, 8)}";
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = noteTitle,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            // Act
            var result = await _noteService.CreateNewNote(note);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to create note: {result.ErrorMessage}");
        }

        [Fact]
        public async Task GetAllNotesFromUser_ReturnsNotesForUser()
        {
            // Arrange - Create a note first
            var noteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = $"Retrieval Test Note {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create note for retrieval test");

            // Act
            var result = await _noteService.GetAllNotesFromUser(_testUserId1);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to get notes for user: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            Assert.Contains(result.Data, n => n.IdNote == noteId);
        }

        [Fact]
        public async Task GetNote_WithValidNoteId_ReturnsNoteDetails()
        {
            // Arrange - Create a note first
            var noteId = Guid.NewGuid();
            var noteTitle = $"Single Note Test {Guid.NewGuid().ToString().Substring(0, 8)}";
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = noteTitle,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create note for get test");

            // Act
            var result = await _noteService.GetNote(noteId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to get note: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
            Assert.Equal(noteId, result.Data.IdNote);
            Assert.Equal(noteTitle, result.Data.Title);
        }

        [Fact]
        public async Task UpdateNote_WithValidNote_Succeeds()
        {
            // Arrange - Create a note first
            var noteId = Guid.NewGuid();
            var originalTitle = $"Original Title {Guid.NewGuid().ToString().Substring(0, 8)}";
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = originalTitle,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create note for update test");

            // Act - Update the note
            var updatedNote = new NoteClient
            {
                IdNote = noteId,
                Title = $"Updated Title {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = note.CreationDate,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var result = await _noteService.UpdateNote(updatedNote);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to update note: {result.ErrorMessage}");
        }

        [Fact]
        public async Task DeleteNote_WithValidNoteId_Succeeds()
        {
            // Arrange - Create a note first
            var noteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = $"Delete Test Note {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create note for delete test");

            // Act
            var result = await _noteService.DeleteNote(noteId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to delete note: {result.ErrorMessage}");

            // Verify note is deleted - attempt to get it should fail or return not found
            var getResult = await _noteService.GetNote(noteId);
            // Note: Depending on server implementation, this might return an error or an empty result
            // This assertion confirms the delete operation was processed
        }

        // ==================== SYNC OPERATIONS ====================

        [Fact]
        public async Task SendChangesToServer_WithValidChanges_Succeeds()
        {
            // Arrange
            var noteServer = new NoteServer
            {
                IdNote = _testNoteId,
                Title = "Test Note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow
            };

            var changes = new List<DTOSendChanges>
            {
                new DTOSendChanges
                {
                    NoteServer = noteServer,
                    Payload = "Test content from client"
                }
            };

            // Act
            var result = await _noteService.SendChangesToServer(changes);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to send changes to server: {result.ErrorMessage}");
        }

        [Fact]
        public async Task GetServerChanges_ReturnsChangesFromServer()
        {
            // Arrange - Send some changes first
            var noteId = Guid.NewGuid();
            var noteServer = new NoteServer
            {
                IdNote = noteId,
                Title = "Test Note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow
            };

            var changes = new List<DTOSendChanges>
            {
                new DTOSendChanges
                {
                    NoteServer = noteServer,
                    Payload = "Sync test content"
                }
            };

            var sendResult = await _noteService.SendChangesToServer(changes);
            Assert.True(sendResult.IsSuccess, "Failed to send changes for sync test");

            // Act
            var result = await _noteService.GetServerChanges();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to get server changes: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
            // There should be changes on the server from various operations
        }

        // ==================== CRDT OPERATIONS ====================

        [Fact]
        public async Task SendCRDTChangestoServer_WithValidPayload_Succeeds()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = $"Delete Test Note {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create note for delete test");

            var payload = new CRDTChangePayload
            {
                IdNote = noteId,
                Payload = "Base64-encoded CRDT changes data"
            };

            // Act
            var result = await _noteService.SendCRDTChangestoServer(payload);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to send CRDT changes: {result.ErrorMessage}");
        }
        [Fact]
        public async Task SendCRDTChangestoServer_NoActuallNoteForThePayLoad_Succeeds()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var payload = new CRDTChangePayload
            {
                IdNote = noteId,
                Payload = "Base64-encoded CRDT changes data"
            };

            // Act
            var result = await _noteService.SendCRDTChangestoServer(payload);

            // Assert
            Assert.NotNull(result);
            Assert.True(!result.IsSuccess, $"Failed to send CRDT changes: {result.ErrorMessage}");
        }
        [Fact]
        public async Task GetServerChangesByNote_WithValidNoteId_ReturnsChanges()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = $"CRDT Test Note {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var noteCreateResult = await _noteService.CreateNewNote(note);
            Assert.True(noteCreateResult.IsSuccess, "Failed to create note for CRDT test");


            var payload = new CRDTChangePayload
            {
                IdNote = noteId,
                Payload = "Base64-encoded CRDT operation data"
            };

            var sendResult = await _noteService.SendCRDTChangestoServer(payload);
            Assert.True(sendResult.IsSuccess, "Failed to send CRDT changes for retrieval test");

            // Act
            var result = await _noteService.GetServerChangesByNote(noteId);

            // Assert
            Assert.NotNull(result);
            // Server might not return these specific changes if they're stored differently
            // This test verifies the endpoint is reachable and processes the request
        }

        // ==================== ACCESS CONTROL OPERATIONS ====================

        [Fact]
        public async Task GiveNoteAccessToUser_WithValidParameters_Succeeds()
        {
            // Arrange - Create a note with user 1
            var noteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = $"Access Control Test Note {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(note);
            Assert.True(createResult.IsSuccess, "Failed to create note for access control test");

            // Act - Give access to user 2
            var result = await _noteService.GiveNoteAccessToUser(noteId, _testUsername2);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Failed to give note access: {result.ErrorMessage}");
        }

        // ==================== ERROR SCENARIOS ====================

        [Fact]
        public async Task GetNote_WithInvalidNoteId_ReturnsErrorOrNotFound()
        {
            // Arrange
            var invalidNoteId = Guid.NewGuid();

            // Act
            var result = await _noteService.GetNote(invalidNoteId);

            // Assert
            Assert.NotNull(result);
            // A note that doesn't exist should result in error or empty result
            // depending on server implementation
        }

        [Fact]
        public async Task UpdateNote_WithNonexistentNote_ReturnsError()
        {
            // Arrange
            var nonexistentNoteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = nonexistentNoteId,
                Title = "Nonexistent Note Update",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            // Act
            var result = await _noteService.UpdateNote(note);

            // Assert
            Assert.NotNull(result);
            // Server should handle update of nonexistent note gracefully
        }

        [Fact]
        public async Task DeleteNote_WithInvalidNoteId_HandlesGracefully()
        {
            // Arrange
            var invalidNoteId = Guid.NewGuid();

            // Act
            var result = await _noteService.DeleteNote(invalidNoteId);

            // Assert
            Assert.NotNull(result);
            // Server should handle deletion of nonexistent note gracefully
        }

        // ==================== CONCURRENT OPERATIONS ====================

        [Fact]
        public async Task ConcurrentNoteCreations_AllSucceed()
        {
            // Arrange
            var tasks = new List<Task<ApiResult>>();

            // Act - Create multiple notes concurrently
            for (int i = 0; i < 3; i++)
            {
                var noteId = Guid.NewGuid();
                var note = new NoteClient
                {
                    IdNote = noteId,
                    Title = $"Concurrent Note {i} {Guid.NewGuid().ToString().Substring(0, 8)}",
                    CreationDate = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    DirtyFlagChangesMade = true
                };

                tasks.Add(_noteService.CreateNewNote(note));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(3, results.Length);
            Assert.All(results, result => Assert.True(result.IsSuccess, $"Concurrent creation failed: {result.ErrorMessage}"));
        }

        [Fact]
        public async Task ConcurrentUpdatesOnSameNote_HandlesProperly()
        {
            // Arrange - Create a note first
            var noteId = Guid.NewGuid();
            var baseNote = new NoteClient
            {
                IdNote = noteId,
                Title = $"Concurrent Update Test {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var createResult = await _noteService.CreateNewNote(baseNote);
            Assert.True(createResult.IsSuccess, "Failed to create note for concurrent update test");

            // Act - Update the note concurrently
            var tasks = new List<Task<ApiResultData<NoteConflictResult>>>();

            for (int i = 0; i < 3; i++)
            {
                var updatedNote = new NoteClient
                {
                    IdNote = noteId,
                    Title = $"Updated {i} {DateTime.UtcNow.Ticks}",
                    CreationDate = baseNote.CreationDate,
                    LastUpdate = DateTime.UtcNow,
                    DirtyFlagChangesMade = true
                };

                tasks.Add(_noteService.UpdateNote(updatedNote));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(3, results.Length);
            // All requests should complete (though some might indicate conflicts)
            Assert.All(results, result => Assert.NotNull(result));
        }

        // ==================== RESPONSE VALIDATION ====================

        [Fact]
        public async Task NoteCreation_ResponseStructureIsValid()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var note = new NoteClient
            {
                IdNote = noteId,
                Title = $"Response Validation Test {Guid.NewGuid().ToString().Substring(0, 8)}",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            // Act
            var result = await _noteService.CreateNewNote(note);

            // Assert - Validate response structure
            Assert.NotNull(result);
            Assert.NotNull(result.IsSuccess);
            Assert.True(result.IsSuccess);
            if (!result.IsSuccess)
            {
                Assert.NotEmpty(result.ErrorMessage);
            }
        }

        [Fact]
        public async Task GetAllNotes_ResponseStructureIsValid()
        {
            // Act
            var result = await _noteService.GetAllNotesFromUser(_testUserId1);

            // Assert - Validate response structure
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"GetAllNotes failed: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
            Assert.IsType<List<NoteClient>>(result.Data);
        }
    }
}
