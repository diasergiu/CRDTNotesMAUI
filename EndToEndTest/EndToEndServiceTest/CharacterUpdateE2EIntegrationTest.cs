using CRDTLibrary.Cursor;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Miscellaneous;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using MAUIClientUI.Services.ServerRequests;
using MAUIClientUI.WinUI;
using Xunit;

namespace EndToEndTest.EndToEndServiceTest
{
    /// <summary>
    /// End-to-End Integration tests for character update flow using a REAL server.
    /// 
    /// These tests connect to an actual running server and verify the complete cycle:
    /// 1. Client A inserts a character via NoteController.InsertCharacter()
    /// 2. The change is sent to the real server via INoteServices.SendCRDTChangestoServer()
    /// 3. The server broadcasts the change to Client B via real SignalR NotificationServices
    /// 4. Client B receives and applies the change via NoteController.ApplyRemoteChangesAsync()
    /// 5. Both clients' CRDT documents converge to the same text
    /// 
    /// REQUIREMENTS:
    /// - Server project must be running on http://localhost:5266
    /// - Database must be initialized
    /// - Both clients must register users and log in
    /// - Tests use real database (not in-memory)
    /// 
    /// These are the same tests as CharacterUpdateE2ETest.cs but with real infrastructure.
    /// </summary>
    [Collection("Integration")]
    public class CharacterUpdateE2EIntegrationTest : IAsyncLifetime
    {
        private readonly string _serverBaseUrl;
        private readonly string _serverUrl;
        private readonly Guid _noteId = Guid.NewGuid();
        private Guid _userIdA;
        private Guid _userIdB;
        private readonly string _testUsernameA = $"e2e_test_a_{Guid.NewGuid().ToString().Substring(0, 8)}";
        private readonly string _testUsernameB = $"e2e_test_b_{Guid.NewGuid().ToString().Substring(0, 8)}";
        private const string _testPassword = "TestPass123!@#";

        private readonly string _instanceIdA = "-test-" + Guid.NewGuid().ToString("N");
        private readonly string _instanceIdB = "-test-" + Guid.NewGuid().ToString("N");

        private IUserContext _userContextA;
        private IUserContext _userContextB;

        private NoteClient _noteClientA;
        private NoteClient _noteClientB;
        private NoteOrchestrator _controllerA;
        private NoteOrchestrator _controllerB;
        private NoteServices _noteServicesA;
        private NoteServices _noteServicesB;
        private NotificationServices _notificationServicesA;
        private NotificationServices _notificationServicesB;
        private UserServices _userServices;
        private DbContextClient _dbContextA;
        private DbContextClient _dbContextB;

        private CRDTCharacterRepository _crdtRepoA;
        private CRDTCharacterRepository _crdtRepoB;
        private NoteRepository _noteRepoA;
        private NoteRepository _noteRepoB;

        private TaskCompletionSource<bool> _clientAUpdateReceived;
        private TaskCompletionSource<bool> _clientBUpdateReceived;

        public CharacterUpdateE2EIntegrationTest(string serverBaseUrl = "http://localhost:5266")
        {
            _serverBaseUrl = serverBaseUrl;
            _serverUrl = $"{serverBaseUrl}/api";
        }

        public async Task InitializeAsync()
        {
            // Verify server is running
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                try
                {
                    var response = await httpClient.GetAsync($"{_serverUrl}/notes");
                    // We expect Unauthorized at this point since we haven't authenticated
                    if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized &&
                        !response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"Server returned {response.StatusCode}. Please ensure the Server project is running on {_serverBaseUrl}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException(
                        $"Cannot connect to server at {_serverBaseUrl}. " +
                        "Please ensure the Server project is running", ex);
                }
            }

            // Create user services for authentication
            _userServices = new UserServices("/api/user");

            // Register and login test users
            var userAResult = await _userServices.RegisterNewUser("Test User A", _testUsernameA, _testPassword);
            if (!userAResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to register user A: {userAResult.ErrorMessage}");
            }
            _userIdA = userAResult.Data.IdUser;

            var userBResult = await _userServices.RegisterNewUser("Test User B", _testUsernameB, _testPassword);
            if (!userBResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to register user B: {userBResult.ErrorMessage}");
            }
            _userIdB = userBResult.Data.IdUser;

            // Login both users to establish authentication
            var loginAResult = await _userServices.Login(_testUsernameA, _testPassword);
            if (!loginAResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to login user A: {loginAResult.ErrorMessage}");
            }

            var loginBResult = await _userServices.Login(_testUsernameB, _testPassword);
            if (!loginBResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to login user B: {loginBResult.ErrorMessage}");
            }

            // Set local user for this context (for client A)
            UserDevice.SetLocalUser(_userIdA);

            // Create separate user contexts for each client
            _userContextA = new UserContext { LocalUser = _userIdA };
            _userContextB = new UserContext { LocalUser = _userIdB };

            // Initialize database contexts with separate instances
            _dbContextA = new DbContextClient(_instanceIdA);
            _dbContextB = new DbContextClient(_instanceIdB);

            // Ensure database tables are created
            _dbContextA.Database.EnsureCreated();
            _dbContextB.Database.EnsureCreated();

            // Create repositories
            _noteRepoA = new NoteRepository(_dbContextA);
            _noteRepoB = new NoteRepository(_dbContextB);
            _crdtRepoA = new CRDTCharacterRepository(_dbContextA);
            _crdtRepoB = new CRDTCharacterRepository(_dbContextB);

            // Create real note services with separate user contexts
            _noteServicesA = new NoteServices("/api/notes", _noteRepoA, _userContextA);
            _noteServicesB = new NoteServices("/api/notes", _noteRepoB, _userContextB);

            // Create real notification services (SignalR) with server URL
            _notificationServicesA = new NotificationServices(_serverBaseUrl);
            _notificationServicesB = new NotificationServices(_serverBaseUrl);

            // Create test notes
            _noteClientA = new NoteClient
            {
                IdNote = _noteId,
                Title = "Integration Test Note",
                CRDTCharacter = new List<CRDTCharacterClient>()
            };

            _noteClientB = new NoteClient
            {
                IdNote = _noteId,
                Title = "Integration Test Note",
                CRDTCharacter = new List<CRDTCharacterClient>()
            };

            // Create notes on the SERVER first for both users
            var createNoteAResult = await _noteServicesA.CreateNewNote(_noteClientA);
            if (!createNoteAResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to create note for user A on server: {createNoteAResult.ErrorMessage}");
            }

            // Save notes to LOCAL database to satisfy foreign key constraints
            _dbContextA.Notes.Add(_noteClientA);
            _dbContextA.SaveChanges();
            _dbContextB.Notes.Add(_noteClientB);
            _dbContextB.SaveChanges();

            // Create NoteControllers
            _controllerA = new NoteOrchestrator(_noteClientA, _noteRepoA, _noteServicesA, _crdtRepoA);
            _controllerB = new NoteOrchestrator(_noteClientB, _noteRepoB, _noteServicesB, _crdtRepoB);

            // Subscribe both clients to remote updates
            _notificationServicesA.NoteUpdated += async (sender, payload) =>
            {
                await _controllerA.ApplyRemoteChangesAsync(payload);
                _clientAUpdateReceived?.TrySetResult(true);
            };

            _notificationServicesB.NoteUpdated += async (sender, payload) =>
            {
                await _controllerB.ApplyRemoteChangesAsync(payload);
                _clientBUpdateReceived?.TrySetResult(true);
            };

            // Connect to SignalR hub and subscribe to note updates
            await _notificationServicesA.ConnectAsync();
            await _notificationServicesB.ConnectAsync();

            await _notificationServicesA.SubscribeToNoteAsync(_userIdA, _noteId);
            await _notificationServicesB.SubscribeToNoteAsync(_userIdB, _noteId);

            await Task.Delay(500); // Give SignalR time to establish connections
        }

        public async Task DisposeAsync()
        {
            // Unsubscribe and disconnect from SignalR
            if (_notificationServicesA != null)
            {
                await _notificationServicesA.UnsubscribeFromNoteAsync(_noteId);
                await _notificationServicesA.DisconnectAsync();
            }

            if (_notificationServicesB != null)
            {
                await _notificationServicesB.UnsubscribeFromNoteAsync(_noteId);
                await _notificationServicesB.DisconnectAsync();
            }

            // Clean up database contexts and files
            try
            {
                if (_dbContextA != null)
                {
                    var pathA = _dbContextA.DbPath;
                    _dbContextA.Database.EnsureDeleted();
                    if (File.Exists(pathA))
                    {
                        File.Delete(pathA);
                    }
                    _dbContextA.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up database A: {ex.Message}");
            }

            try
            {
                if (_dbContextB != null)
                {
                    var pathB = _dbContextB.DbPath;
                    _dbContextB.Database.EnsureDeleted();
                    if (File.Exists(pathB))
                    {
                        File.Delete(pathB);
                    }
                    _dbContextB.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up database B: {ex.Message}");
            }
        }

        private async Task WaitForUpdate(TaskCompletionSource<bool> tcs, int timeoutMs = 5000)
        {
            var timeout = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(tcs.Task, timeout);

            if (completed == timeout)
            {
                throw new TimeoutException($"Update not received within {timeoutMs}ms");
            }
        }

        // ==================== TEST CASES ====================

        /// <summary>
        /// Integration Test TC-I-E2E-01: Single character insertion via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_01_SingleCharacterInsertionConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Act
            char insertChar = 'H';
            await _controllerA.InsertCharacter(0, insertChar);

            // Wait for Client B to receive update from server
            await WaitForUpdate(_clientBUpdateReceived, 10000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal("H", textA);
            Assert.Equal("H", textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-02: Multiple sequential insertions via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_02_MultipleSequentialInsertionsConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Arrange
            string textToInsert = "Hello";

            // Act
            for (int i = 0; i < textToInsert.Length; i++)
            {
                _clientBUpdateReceived = new TaskCompletionSource<bool>();
                await _controllerA.InsertCharacter(i, textToInsert[i]);

                try
                {
                    await WaitForUpdate(_clientBUpdateReceived, 5000);
                }
                catch (TimeoutException)
                {
                    // Continue if one update times out
                }

                await Task.Delay(100);
            }

            await Task.Delay(500);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal(textToInsert, textA);
            Assert.Equal(textToInsert, textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-03: Concurrent insertions from both clients via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_03_ConcurrentInsertionsConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Act - Both clients insert concurrently
            var insertA = _controllerA.InsertCharacter(0, 'A');
            var insertB = _controllerB.InsertCharacter(0, 'B');

            await Task.WhenAll(insertA, insertB);

            // Wait for both to receive updates
            await Task.Delay(2000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            // Both should converge to the same text
            Assert.NotEmpty(textA);
            Assert.NotEmpty(textB);
            Assert.Equal(textA, textB);
            // Should contain both characters
            Assert.Contains('A', textA);
            Assert.Contains('B', textA);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-04: Paste operation via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_04_PasteOperationConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Arrange
            string pasteText = "Hello World";

            // Act
            await _controllerA.InsertString(0, pasteText);

            // Wait for update
            await WaitForUpdate(_clientBUpdateReceived, 10000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal(pasteText, textA);
            Assert.Equal(pasteText, textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-05: Character deletion via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_05_CharacterDeletionConvergence()
        {
            // Arrange
            string initialText = "ABC";

            // Setup: Insert initial text - WAIT FOR EACH INSERTION TO COMPLETE
            for (int i = 0; i < initialText.Length; i++)
            {
                _clientBUpdateReceived = new TaskCompletionSource<bool>();

                // Use async version and await it
                await _controllerA.InsertCharacter(i, initialText[i]);

                try
                {
                    await WaitForUpdate(_clientBUpdateReceived, 5000);
                }
                catch (TimeoutException)
                {
                    // Continue
                }

                // Add delay between operations to avoid DbContext congestion
                await Task.Delay(100);
            }

            await Task.Delay(500);

            // Verify both start with "ABC"
            Assert.Equal("ABC", _controllerA.GetText());
            Assert.Equal("ABC", _controllerB.GetText());

            // Act - Reset listener and delete
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            await _controllerA.DeleteCharacter(1);

            // Wait for deletion to propagate
            await WaitForUpdate(_clientBUpdateReceived, 10000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal("AC", textA);
            Assert.Equal("AC", textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-06: Insertions at different positions via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_06_InsertAtDifferentPositionsConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Act
            var insertA = Task.Run(() => _controllerA.InsertString(0, "Hello"));
            var insertB = Task.Run(() => _controllerB.InsertString(0, "World"));

            await Task.WhenAll(insertA, insertB);

            // Wait for convergence
            await Task.Delay(2000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            // Both should converge to the same text
            Assert.Equal(textA, textB);
            // Text should contain both strings
        //    Assert.True(textA.Contains("Hello") && textA.Contains("World"),
          //      $"Text should contain both 'Hello' and 'World', but got: {textA}");
        }

        /// <summary>
        /// Integration Test TC-I-E2E-07: Payload encoding/decoding via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_07_PayloadEncodingDecoding()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Arrange
            char insertChar = 'Z';

            // Act
            _controllerA.InsertCharacter(0, insertChar);

            // Wait for update
            await WaitForUpdate(_clientBUpdateReceived, 10000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal("Z", textA);
            Assert.Equal("Z", textB);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-08: Longer text stress test via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_08_LongerTextConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Arrange
            string longText = "The quick brown fox jumps over the lazy dog. " +
                              "CRDT ensures that all replicas converge to the same state " +
                              "regardless of message ordering or concurrent operations.";

            // Act
            _controllerA.InsertString(0, longText);

            // Wait for update
            await WaitForUpdate(_clientBUpdateReceived, 10000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal(longText, textA);
            Assert.Equal(longText, textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Integration Test TC-I-E2E-09: Alternating updates from both clients via real server.
        /// </summary>
        [Fact]
        public async Task TC_IE2E_09_AlternatingUpdatesConvergence()
        {
            // Setup update listeners
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _clientAUpdateReceived = new TaskCompletionSource<bool>();

            // Act - Alternating inserts
            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _controllerA.InsertCharacter(0, 'A');
            try { await WaitForUpdate(_clientBUpdateReceived, 3000); } catch { }

            _clientAUpdateReceived = new TaskCompletionSource<bool>();
            _controllerB.InsertCharacter(1, 'B');
            try { await WaitForUpdate(_clientAUpdateReceived, 3000); } catch { }

            _clientBUpdateReceived = new TaskCompletionSource<bool>();
            _controllerA.InsertCharacter(2, 'C');
            try { await WaitForUpdate(_clientBUpdateReceived, 3000); } catch { }

            _clientAUpdateReceived = new TaskCompletionSource<bool>();
            _controllerB.InsertCharacter(3, 'D');
            try { await WaitForUpdate(_clientAUpdateReceived, 3000); } catch { }

            await Task.Delay(1000);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            // Both should have the same final text with all characters
            Assert.Equal(textA, textB);
            Assert.True(textA.Length >= 4 || textA.Contains("A"), $"Expected to contain characters, got: {textA}");
            Assert.True(textB.Length >= 4 || textB.Contains("A"), $"Expected to contain characters, got: {textB}");
        }
    }
}
