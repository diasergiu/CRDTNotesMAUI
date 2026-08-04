using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.ServeRepositories;
using Xunit;

namespace Server.Test
{
    public class ServerIntegrationTests
    {
        private readonly DbContextServer _dbContext;
        private readonly NotesRepository _repository;
        private readonly UserController _userController;
        private readonly NotesController _notesController;

        public ServerIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new DbContextServer(options);
            _repository = new NotesRepository(_dbContext, null);
            _userController = new UserController(_dbContext, _repository);
            _notesController = new NotesController(_dbContext, _repository);
        }

        #region User Registration and Login Flow

        [Fact]
        public async Task UserRegistration_ThenLogin_SequenceSucceeds()
        {
            // Arrange
            var registerRequest = new UserController.RegisterRequest
            {
                Name = "Integration Test User",
                Username = "integrationuser",
                Password = "testpass123"
            };

            // Act - Register new user
            var registerResult = await _userController.Register(registerRequest);

            // Assert - Registration succeeded
            Assert.NotNull(registerResult);
            var registerOkResult = registerResult as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.NotNull(registerOkResult);

            var registeredUserId = ((dynamic)registerOkResult.Value).IdUser;
            Assert.NotEqual(Guid.Empty, registeredUserId);

            // Act - Login with registered credentials
            var loginResult = await _userController.Login(registerRequest.Username, registerRequest.Password);

            // Assert - Login succeeded and returned same user ID
            Assert.NotNull(loginResult);
            var loginOkResult = loginResult as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.NotNull(loginOkResult);

            var loginUserId = ((dynamic)loginOkResult.Value).IdUser;
            Assert.Equal(registeredUserId, loginUserId);
        }

        #endregion

        #region Note Creation and Retrieval Flow

        [Fact]
        public async Task CreateNote_ThenRetrieve_ReturnsCreatedNote()
        {
            // Arrange - Create and login user
            var userId = Guid.NewGuid();
            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = "testuser",
                Password = "password123"
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var noteClient = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "Integration Test Note",
                Content = "This is test content",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1,
                DirtyFlagChangesMade = true
            };

            // Act - Create note
            var createdNote = await _repository.CreateNote(noteClient, userId);

            // Assert - Note was created
            Assert.NotNull(createdNote);
            Assert.Equal(noteClient.Title, createdNote.Title);
            Assert.Equal(noteClient.Content, createdNote.Content);

            // Act - Retrieve user notes
            var userNotes = await _repository.GetAllNotesFromUser(userId);

            // Assert - Note is in user's notes
            Assert.NotEmpty(userNotes);
            Assert.Single(userNotes);
            Assert.Equal(createdNote.IdNote, userNotes.First().IdNote);
        }

        [Fact]
        public async Task CreateMultipleNotes_ThenRetrieve_ReturnsAllNotes()
        {
            // Arrange - Create user
            var userId = Guid.NewGuid();
            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = "testuser",
                Password = "password123"
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var note1 = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "First Note",
                Content = "First content",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1,
                DirtyFlagChangesMade = true
            };

            var note2 = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "Second Note",
                Content = "Second content",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1,
                DirtyFlagChangesMade = true
            };

            // Act - Create multiple notes
            await _repository.CreateNote(note1, userId);
            await _repository.CreateNote(note2, userId);

            // Act - Retrieve all user notes
            var userNotes = await _repository.GetAllNotesFromUser(userId);

            // Assert - All notes are returned
            Assert.NotEmpty(userNotes);
            Assert.Equal(2, userNotes.Count);
        }

        #endregion

        #region Note Update and Delete Flow

        #endregion

        #region Multi-User Isolation Tests

        [Fact]
        public async Task MultipleUsers_NoteIsolation_UsersCannotAccessEachOtherNotes()
        {
            // Arrange - Create two users
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();

            var user1 = new UserServer
            {
                IdUser = user1Id,
                Name = "User One",
                Username = "userone",
                Password = "pass123"
            };

            var user2 = new UserServer
            {
                IdUser = user2Id,
                Name = "User Two",
                Username = "usertwo",
                Password = "pass456"
            };

            _dbContext.Users.Add(user1);
            _dbContext.Users.Add(user2);
            await _dbContext.SaveChangesAsync();

            // Arrange - Create note for user 1
            var note1 = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "User 1 Note",
                Content = "Only user 1 should see this",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1,
                DirtyFlagChangesMade = true
            };

            var note2 = new NoteClient
            {
                IdNote = Guid.NewGuid(),
                Title = "User 2 Note",
                Content = "Only user 2 should see this",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1,
                DirtyFlagChangesMade = true
            };

            await _repository.CreateNote(note1, user1Id);
            await _repository.CreateNote(note2, user2Id);

            // Act - Retrieve each user's notes
            var user1Notes = await _repository.GetAllNotesFromUser(user1Id);
            var user2Notes = await _repository.GetAllNotesFromUser(user2Id);

            // Assert - Each user only sees their own notes
            Assert.Single(user1Notes);
            Assert.Single(user2Notes);
            Assert.Equal("User 1 Note", user1Notes.First().Title);
            Assert.Equal("User 2 Note", user2Notes.First().Title);
            Assert.DoesNotContain(user1Notes, n => n.Title == "User 2 Note");
            Assert.DoesNotContain(user2Notes, n => n.Title == "User 1 Note");
        }

        #endregion
    }
}
