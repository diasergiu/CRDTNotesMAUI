using DatabaseLibrary.Entities.Client;
using EndToEndTest.Mocks;
using MAUIClientUI.Miscellaneous;
using MAUIClientUI.Services.HelperClasses;

namespace EndToEndTest.EndToEndServiceTest
{
    /// <summary>
    /// End-to-End tests for character update flow between two clients using CRDT.
    /// 
    /// Tests verify the complete cycle:
    /// 1. Client A inserts a character via NoteController.InsertCharacter()
    /// 2. The change is sent to the server via INoteServices.SendCRDTChangestoServer()
    /// 3. The server broadcasts the change to Client B via NotificationServices.NoteUpdated event
    /// 4. Client B receives and applies the change via NoteController.ApplyRemoteChangesAsync()
    /// 5. Both clients' CRDT documents converge to the same text
    /// 
    /// </summary>
    public class CharacterUpdateE2ETest : IAsyncLifetime
    {
        private readonly Guid _noteId = Guid.NewGuid();
        private readonly Guid _clientAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private readonly Guid _clientBId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private NoteClient _noteClientA;
        private NoteClient _noteClientB;
        private NoteOrchestrator _controllerA;
        private NoteOrchestrator _controllerB;
        private MockNoteServices _mockNoteServices;
        private MockNotificationServices _mockNotificationServices;
        private DbContextClient _dbContextA;
        private DbContextClient _dbContextB;

        public async Task InitializeAsync()
        {
            // Initialize test fixture - called once before test methods run
            await Setup();
        }

        public async Task DisposeAsync()
        {
            // Cleanup after all tests complete
            Cleanup();
            await Task.CompletedTask;
        }

        private async Task Setup()
        {
            // Create mock services (shared between clients for this test)
            _mockNoteServices = new MockNoteServices();
            _mockNotificationServices = new MockNotificationServices();

            // Create mock repositories for both clients
            var mockNoteRepoA = new MockNoteRepository();
            var mockNoteRepoB = new MockNoteRepository();
            var mockCRDTRepoA = new MockCRDTCharacterRepository();
            var mockCRDTRepoB = new MockCRDTCharacterRepository();

            // Create test notes for both clients
            _noteClientA = new NoteClient
            {
                IdNote = _noteId,
                Title = "Test Note",
                CRDTCharacter = new List<CRDTCharacterClient>()
            };

            _noteClientB = new NoteClient
            {
                IdNote = _noteId,
                Title = "Test Note",
                CRDTCharacter = new List<CRDTCharacterClient>()
            };

            // Create database contexts (or mock them if needed)
            _dbContextA = new DbContextClient();
            _dbContextB = new DbContextClient();

            // Create NoteControllers for both clients using mock repositories
            _controllerA = new NoteOrchestrator(_noteClientA, mockNoteRepoA, _mockNoteServices, mockCRDTRepoA);
            _controllerB = new NoteOrchestrator(_noteClientB, mockNoteRepoB, _mockNoteServices, mockCRDTRepoB);

            // Wire up bi-directional communication:
            // When either client sends a change, the mock service broadcasts to both clients
            _mockNoteServices.OnCRDTChangeSent = async (payload) =>
            {
                // Simulate server receiving from client and broadcasting to all subscribers
                if (payload.IdNote == _noteId)
                {
                    // Simulate the async broadcast to all clients subscribed to this note
                    await _mockNotificationServices.SimulateRemoteUpdateAsync(payload);
                }
            };

            // Subscribe BOTH clients to remote updates (simulating real-world behavior)
            // In a real app, both clients would be subscribed to the NotificationServices
            _mockNotificationServices.NoteUpdated += async (sender, payload) =>
            {
                // Apply to Client A (unless it was the sender)
                // In this test, we always apply since we're simulating both receiving server updates
                await _controllerA.ApplyRemoteChangesAsync(payload);
            };

            _mockNotificationServices.NoteUpdated += async (sender, payload) =>
            {
                // Apply to Client B (unless it was the sender)
                await _controllerB.ApplyRemoteChangesAsync(payload);
            };

            await Task.CompletedTask;
        }

        private void Cleanup()
        {
            _mockNoteServices?.Clear();
            _mockNotificationServices?.ClearSubscribers();
            _dbContextA?.Dispose();
            _dbContextB?.Dispose();
        }

        // ==================== TEST CASES ====================

        /// <summary>
        /// Test TC-E2E-01: Single character insertion from one client is received and applied by another client.
        /// </summary>
        [Fact]
        public async Task TC_E2E_01_SingleCharacterInsertionConvergence()
        {
            // Arrange
            char insertChar = 'H';
            int position = 0;

            // Act
            _controllerA.InsertCharacter(position, insertChar);
            await Task.Delay(100);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal("H", textA);
            Assert.Equal("H", textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Test TC-E2E-02: Multiple sequential character insertions converge across clients.
        /// </summary>
        [Fact]
        public async Task TC_E2E_02_MultipleSequentialInsertionsConvergence()
        {
            // Arrange
            string textToInsert = "Hello";

            // Act
            for (int i = 0; i < textToInsert.Length; i++)
            {
                _controllerA.InsertCharacter(i, textToInsert[i]);
                await Task.Delay(10);
            }

            await Task.Delay(100);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal(textToInsert, textA);
            Assert.Equal(textToInsert, textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Test TC-E2E-03: Concurrent insertions from two clients converge (CRDT property).
        /// </summary>
        [Fact]
        public async Task TC_E2E_03_ConcurrentInsertionsConvergence()
        {
            // Act
            _controllerA.InsertCharacter(1, 'A');
            _controllerB.InsertCharacter(1, 'B');

            await Task.Delay(100);

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
        /// Test TC-E2E-04: Paste operation (multiple characters at once) propagates correctly.
        /// </summary>
        [Fact]
        public async Task TC_E2E_04_PasteOperationConvergence()
        {
            // Arrange
            string pasteText = "Hello World";

            // Act
            _controllerA.InsertString(0, pasteText);
            await Task.Delay(100);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal(pasteText, textA);
            Assert.Equal(pasteText, textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Test TC-E2E-05: Character deletion is propagated and converges across clients.
        /// </summary>
        [Fact]
        public async Task TC_E2E_05_CharacterDeletionConvergence()
        {
            // Arrange
            string initialText = "ABC";

            // Setup: Insert initial text
            for (int i = 0; i < initialText.Length; i++)
            {
                _controllerA.InsertCharacter(i, initialText[i]);
            }

            await Task.Delay(100);

            // Verify both start with "ABC"
            Assert.Equal("ABC", _controllerA.GetText());
            Assert.Equal("ABC", _controllerB.GetText());

            // Act
            _controllerA.DeleteCharacter(1);
            await Task.Delay(100);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal("AC", textA);
            Assert.Equal("AC", textB);
            Assert.Equal(textA, textB);
        }

        /// <summary>
        /// Test TC-E2E-06: Multiple clients inserting at different positions converge correctly.
        /// </summary>
        [Fact]
        public async Task TC_E2E_06_InsertAtDifferentPositionsConvergence()
        {
            // Act
            _controllerA.InsertString(0, "Hello");
            _controllerB.InsertString(0, "World");

            await Task.Delay(100);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            // Both should converge to the same text
            Assert.Equal(textA, textB);
            // Text should contain both strings
            Assert.True(textA.Contains("Hello") && textA.Contains("World"), 
                $"Text should contain both 'Hello' and 'World', but got: {textA}");
        }

        /// <summary>
        /// Test TC-E2E-07: Verify that the sent change payload is correctly encoded and decoded.
        /// </summary>
        [Fact]
        public async Task TC_E2E_07_PayloadEncodingDecoding()
        {
            // Arrange
            char insertChar = 'Z';

            // Act
            _controllerA.InsertCharacter(0, insertChar);
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(_mockNoteServices.SentChanges);

            var payload = _mockNoteServices.SentChanges.First();
            Assert.Equal(_noteId, payload.IdNote);
            Assert.NotEmpty(payload.Payload);

            // Verify the payload can be decoded
            var decodedCharacters = CharacterSerializer.Decode(payload.Payload);
            Assert.NotEmpty(decodedCharacters);

            // Verify the decoded character matches what was sent
            var decodedChar = decodedCharacters.First();
            Assert.Equal(insertChar, decodedChar.Character);

            // Verify both clients converged
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();
            Assert.Equal("Z", textA);
            Assert.Equal("Z", textB);
        }

        /// <summary>
        /// Test TC-E2E-08: Verify that multiple payloads are tracked correctly.
        /// </summary>
        [Fact]
        public async Task TC_E2E_08_MultiplePayloadsTracking()
        {
            // Act
            _controllerA.InsertCharacter(0, 'A');
            await Task.Delay(10);
            _controllerA.InsertCharacter(1, 'B');
            await Task.Delay(10);
            _controllerA.InsertCharacter(2, 'C');

            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(_mockNoteServices.SentChanges);

            // All payloads should be for the correct note
            foreach (var payload in _mockNoteServices.SentChanges)
            {
                Assert.Equal(_noteId, payload.IdNote);
                Assert.NotEmpty(payload.Payload);
            }

            // Verify convergence
            Assert.Equal("ABC", _controllerA.GetText());
            Assert.Equal("ABC", _controllerB.GetText());
        }

        /// <summary>
        /// Test TC-E2E-09: Stress test with longer text to verify scalability.
        /// </summary>
        [Fact]
        public async Task TC_E2E_09_LongerTextConvergence()
        {
            // Arrange
            string longText = "The quick brown fox jumps over the lazy dog. " +
                              "CRDT ensures that all replicas converge to the same state " +
                              "regardless of message ordering or concurrent operations.";

            // Act
            _controllerA.InsertString(0, longText);
            await Task.Delay(200);

            // Assert
            string textA = _controllerA.GetText();
            string textB = _controllerB.GetText();

            Assert.Equal(longText, textA);
            Assert.Equal(longText, textB);
            Assert.Equal(textA, textB);
        }
    }
}
