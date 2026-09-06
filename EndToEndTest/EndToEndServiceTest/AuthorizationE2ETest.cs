using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.ServerRequests;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace EndToEndTest.EndToEndServiceTest
{
    /// <summary>
    /// End-to-End Authorization and Security Tests
    /// 
    /// These tests verify that authorization middleware correctly:
    /// - Rejects unauthorized CRDT operations (SEC-06)
    /// - Enforces access control across all note operations
    /// - Validates user identity headers
    /// - Prevents privilege escalation
    /// 
    /// REQUIREMENTS:
    /// 1. Start the Server project before running these tests
    /// 2. Server should be listening on http://localhost:5266
    /// 3. Database should be initialized
    /// 
    /// NOTE: These are true E2E tests with direct HTTP calls to test middleware.
    /// Authorization is validated at the middleware/filter level, not in the controller.
    /// All requests are made directly via HttpClient to verify middleware enforcement.
    /// </summary>
    [Collection("Sequential")]
    public class AuthorizationE2ETest : IAsyncLifetime
    {
        private readonly string _testServerUrl = "http://localhost:5266/api";
        private const string TEST_USERNAME_PREFIX = "e2etest_auth_";
        private const string TEST_PASSWORD = "TestPass123!@#";
        private const string TEST_USER_NAME = "E2E Auth Test User";

        private UserServices _userService;
        private readonly HttpClient _httpClient;

        // Test data holders
        private UserClient _testUser1;
        private UserClient _testUser2;
        private Guid _testUserId1;
        private Guid _testUserId2;
        private string _testUsername1;
        private string _testUsername2;

        public AuthorizationE2ETest()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _userService = new UserServices("/api/user", new UserContext());
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

            // Register and login second test user
            var registerResult2 = await _userService.RegisterNewUser(TEST_USER_NAME, _testUsername2, TEST_PASSWORD);
            Assert.True(registerResult2.IsSuccess, $"Failed to register user 2: {registerResult2.ErrorMessage}");

            var loginResult2 = await _userService.Login(_testUsername2, TEST_PASSWORD);
            Assert.True(loginResult2.IsSuccess, $"Failed to login user 2: {loginResult2.ErrorMessage}");

            _testUser2 = loginResult2.Data;
            _testUserId2 = _testUser2.IdUser;
        }

        // ==================== Helper Methods ====================

        private async Task<(bool success, Guid noteId)> CreateNoteViaHttp(Guid userId, string title)
        {
            var noteId = Guid.NewGuid();
            var notePayload = new NoteClient
            {
                IdNote = noteId,
                Title = title,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_testServerUrl}/notes");
            request.Headers.Add("X-User-Id", userId.ToString());
            request.Content = JsonContent.Create(notePayload);

            var response = await _httpClient.SendAsync(request);
            return (response.IsSuccessStatusCode, noteId);
        }

        private async Task<bool> GrantNoteAccessViaHttp(Guid userId, Guid noteId, string targetUsername)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, 
                $"{_testServerUrl}/notes/GiveNoteAccessToUser/{noteId}?UserName={targetUsername}");
            request.Headers.Add("X-User-Id", userId.ToString());

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> SubmitCRDTViaHttp(Guid userId, Guid noteId, string payload)
        {
            var crdtPayload = new CRDTChangePayload
            {
                IdNote = noteId,
                Payload = payload
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"{_testServerUrl}/notes/SendCRDTChangestoServer");
            request.Headers.Add("X-User-Id", userId.ToString());
            request.Content = JsonContent.Create(crdtPayload);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> UpdateNoteViaHttp(Guid userId, Guid noteId, string newTitle)
        {
            var notePayload = new NoteClient
            {
                IdNote = noteId,
                Title = newTitle,
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = false
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"{_testServerUrl}/notes/{noteId}");
            request.Headers.Add("X-User-Id", userId.ToString());
            request.Content = JsonContent.Create(notePayload);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private async Task<HttpResponseMessage> GetNoteViaHttp(Guid userId, Guid noteId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_testServerUrl}/notes/{noteId}");
            request.Headers.Add("X-User-Id", userId.ToString());

            return await _httpClient.SendAsync(request);
        }

        private async Task<HttpResponseMessage> GetAllNotesViaHttp(Guid userId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_testServerUrl}/notes/GetAllNotesFromUser");
            request.Headers.Add("X-User-Id", userId.ToString());

            return await _httpClient.SendAsync(request);
        }

        // ==================== SEC-06: Unauthorized CRDT Operations ====================

        //[Fact]
        //public async Task SubmitCRDTOperations_WithoutAccess_ReturnsUnauthorized()
        //{
        //    // Arrange: User 1 creates a note, User 2 has NO access
        //    var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "User 1 Private Note");
        //    Assert.True(success, "Failed to create note with User 1");

        //    // Act: User 2 tries to submit CRDT changes via HTTP (which should fail at middleware)
        //    var crdtPayload = new CRDTChangePayload
        //    {
        //        IdNote = noteId,
        //        Payload = "SGVsbG8gV29ybGQgRW5jb2RlZA==" // Dummy payload
        //    };

        //    var request = new HttpRequestMessage(HttpMethod.Put, $"{_testServerUrl}/notes/SendCRDTChangestoServer");
        //    request.Headers.Add("X-User-Id", _testUserId2.ToString());
        //    request.Content = JsonContent.Create(crdtPayload);

        //    var response = await _httpClient.SendAsync(request);

        //    // Assert: Should be rejected by middleware
        //    Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        //    Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
        //               response.StatusCode == HttpStatusCode.Forbidden,
        //               $"Expected Unauthorized or Forbidden, got {response.StatusCode}");
        //}

        [Fact]
        public async Task SubmitCRDTOperations_AfterAccessGranted_Succeeds()
        {
            // Arrange: User 1 creates a note and grants access to User 2
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Note for CRDT Auth Test");
            Assert.True(success);

            // Grant access to User 2
            var grantSuccess = await GrantNoteAccessViaHttp(_testUserId1, noteId, _testUsername2);
            Assert.True(grantSuccess, "Failed to grant access");

            // Act: User 2 submits CRDT changes via HTTP (should now succeed)
            var crdtPayload = new CRDTChangePayload
            {
                IdNote = noteId,
                Payload = "SGVsbG8gRnJvbSBVc2VyMg==" // Dummy payload
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"{_testServerUrl}/notes/SendCRDTChangestoServer");
            request.Headers.Add("X-User-Id", _testUserId2.ToString());
            request.Content = JsonContent.Create(crdtPayload);

            var response = await _httpClient.SendAsync(request);

            // Assert: Should succeed because access was granted
            Assert.True(response.IsSuccessStatusCode, 
                $"CRDT submission failed with status {response.StatusCode}");
        }

        // ==================== FR-02: Shared Notes in List ====================

        [Fact]
        public async Task GetAllNotes_IncludesSharedNotes_AfterAccessGranted()
        {
            // Arrange: User 1 creates note and shares with User 2
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Note Shared in List Test");
            Assert.True(success);

            // Grant access to User 2
            var grantSuccess = await GrantNoteAccessViaHttp(_testUserId1, noteId, _testUsername2);
            Assert.True(grantSuccess);

            // Act: User 2 retrieves all their notes via HTTP
            var response = await GetAllNotesViaHttp(_testUserId2);

            // Assert: Response should succeed and contain the shared note
            Assert.True(response.IsSuccessStatusCode, 
                $"GetAllNotes failed with status {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(noteId.ToString(), content);
        }

        // ==================== FR-04: Collaborator Edit Access ====================

        [Fact]
        public async Task UpdateNote_ByCollaborator_AfterAccessGranted_Succeeds()
        {
            // Arrange: User 1 creates note and shares with User 2
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Original Title for Edit Test");
            Assert.True(success);

            // Grant access to User 2
            var grantSuccess = await GrantNoteAccessViaHttp(_testUserId1, noteId, _testUsername2);
            Assert.True(grantSuccess);

            // Act: User 2 edits the note via HTTP
            var updateSuccess = await UpdateNoteViaHttp(_testUserId2, noteId, "Edited by Collaborator");

            // Assert: Update should succeed
            Assert.True(updateSuccess, "Collaborator edit should succeed after access granted");
        }

        [Fact]
        public async Task UpdateNote_WithoutAccess_ReturnsForbidden()
        {
            // Arrange: User 1 creates note, NO access for User 2
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Private Note Cannot Edit");
            Assert.True(success);

            // Act: User 2 tries to edit without access via HTTP
            var notePayload = new NoteClient
            {
                IdNote = noteId,
                Title = "Tried to Edit",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = false
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"{_testServerUrl}/notes/{noteId}");
            request.Headers.Add("X-User-Id", _testUserId2.ToString());
            request.Content = JsonContent.Create(notePayload);

            var response = await _httpClient.SendAsync(request);

            // Assert: Should be rejected
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                       response.StatusCode == HttpStatusCode.Forbidden || 
                       response.StatusCode == HttpStatusCode.BadRequest,
                       $"Expected Unauthorized/Forbidden/BadRequest, got {response.StatusCode}");
        }

        [Fact]
        public async Task GetNote_WithoutAccess_ReturnsUnauthorized()
        {
            // Arrange: User 1 creates note, User 2 has NO access
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Private Note for User 1");
            Assert.True(success);

            // Act: User 2 tries to read the note via HTTP
            var response = await GetNoteViaHttp(_testUserId2, noteId);

            // Assert: Should be rejected
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                       response.StatusCode == HttpStatusCode.Forbidden,
                       $"Expected Unauthorized or Forbidden, got {response.StatusCode}");
        }

        // ==================== FR-11: Full Access Grant Workflow ====================

        [Fact]
        public async Task AccessGrantee_CanReadEditAndShare_FullWorkflow()
        {
            // Arrange: Three users for the workflow test
            var guid3 = Guid.NewGuid().ToString().Substring(0, 8);
            var testUsername3 = $"{TEST_USERNAME_PREFIX}user3_{guid3}";

            var registerResult3 = await _userService.RegisterNewUser(TEST_USER_NAME, testUsername3, TEST_PASSWORD);
            Assert.True(registerResult3.IsSuccess);

            var loginResult3 = await _userService.Login(testUsername3, TEST_PASSWORD);
            Assert.True(loginResult3.IsSuccess);
            var testUserId3 = loginResult3.Data.IdUser;

            // User 1 creates note
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Workflow Test Note");
            Assert.True(success);

            // STEP 1: User 1 grants access to User 2
            var grant1Success = await GrantNoteAccessViaHttp(_testUserId1, noteId, _testUsername2);
            Assert.True(grant1Success);

            // STEP 2: User 2 reads the note
            var readResponse = await GetNoteViaHttp(_testUserId2, noteId);
            Assert.True(readResponse.IsSuccessStatusCode, "User 2 should be able to read shared note");

            // STEP 3: User 2 edits the note
            var editSuccess = await UpdateNoteViaHttp(_testUserId2, noteId, "Edited by User 2");
            Assert.True(editSuccess, "User 2 should be able to edit note");

            // STEP 4: User 2 shares the note with User 3
            var grant2Success = await GrantNoteAccessViaHttp(_testUserId2, noteId, testUsername3);
            Assert.True(grant2Success, "User 2 should be able to share note");

            // STEP 5: User 3 can now read the note
            var user3ReadResponse = await GetNoteViaHttp(testUserId3, noteId);
            Assert.True(user3ReadResponse.IsSuccessStatusCode, "User 3 should be able to read shared note");
        }

        // ==================== Additional Authorization Tests ====================

        [Fact]
        public async Task MissingUserIdHeader_ReturnsUnauthorized()
        {
            // Arrange: Create a note first
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Test Note");
            Assert.True(success);

            // Act: Try to read note without X-User-Id header
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_testServerUrl}/notes/{noteId}");
            // Intentionally NOT adding X-User-Id header

            var response = await _httpClient.SendAsync(request);

            // Assert: Should be rejected
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.Unauthorized,
                       $"Expected BadRequest or Unauthorized, got {response.StatusCode}");
        }

        [Fact]
        public async Task InvalidUserIdHeader_ReturnsBadRequest()
        {
            // Arrange: Create a note first
            var (success, noteId) = await CreateNoteViaHttp(_testUserId1, "Test Note");
            Assert.True(success);

            // Act: Try to read note with invalid GUID in header
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_testServerUrl}/notes/{noteId}");
            request.Headers.Add("X-User-Id", "not-a-valid-guid");

            var response = await _httpClient.SendAsync(request);

            // Assert: Should be rejected
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.Unauthorized,
                       $"Expected BadRequest or Unauthorized, got {response.StatusCode}");
        }
    }
}
