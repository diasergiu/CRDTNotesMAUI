using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Server.ServeRepositories;
using Server.ServerHub;

namespace Server.Test
{
    public class NotesRepositoryTests
    {
        private readonly DbContextServer _dbContext;
        private readonly NotesRepository _repository;

        public NotesRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new DbContextServer(options);
            _repository = new NotesRepository(_dbContext);
        }

        #region GetUser Tests

        [Fact]
        public async Task GetUser_WithValidCredentials_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";
            var password = "password123";

            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = username,
                Password = password
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.getUser(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.IdUser);
            Assert.Equal(username, result.Username);
        }

        [Fact]
        public async Task GetUser_WithInvalidUsername_ReturnsNull()
        {
            // Arrange
            var username = "nonexistentuser";
            var password = "password123";

            // Act
            var result = await _repository.getUser(username, password);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUser_WithInvalidPassword_ReturnsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";
            var password = "password123";

            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = username,
                Password = password
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.getUser(username, "wrongpassword");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllNotesFromUser Tests

        [Fact]
        public async Task GetAllNotesFromUser_WithValidUserId_ReturnsUserNotes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var noteId1 = Guid.NewGuid();
            var noteId2 = Guid.NewGuid();

            var note1 = new NoteServer
            {
                IdNote = noteId1,
                Title = "Note 1",
                Content = "Content 1",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1
            };

            var note2 = new NoteServer
            {
                IdNote = noteId2,
                Title = "Note 2",
                Content = "Content 2",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version
                = 1
            };

            _dbContext.Notes.AddRange(note1, note2);

            var noteUser1 = new Note_UserServer { IdNote = noteId1, IdUser = userId };
            var noteUser2 = new Note_UserServer { IdNote = noteId2, IdUser = userId };

            _dbContext.Note_Users.AddRange(noteUser1, noteUser2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllNotesFromUser(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllNotesFromUser_WithNoNotes_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.GetAllNotesFromUser(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllNotesFromUser_WithMultipleUsers_ReturnsOnlyUserNotes()
        {
            // Arrange
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var noteId1 = Guid.NewGuid();
            var noteId2 = Guid.NewGuid();

            var note1 = new NoteServer
            {
                IdNote = noteId1,
                Title = "User 1 Note",
                Content = "Content 1",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1
            };

            var note2 = new NoteServer
            {
                IdNote = noteId2,
                Title = "User 2 Note",
                Content = "Content 2",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1
            };

            _dbContext.Notes.AddRange(note1, note2);

            _dbContext.Note_Users.AddRange(
                new Note_UserServer { IdNote = noteId1, IdUser = userId1 },
                new Note_UserServer { IdNote = noteId2, IdUser = userId2 }
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllNotesFromUser(userId1);

            // Assert
            Assert.Single(result);
            Assert.Equal(noteId1, result[0].IdNote);
        }

        #endregion

        #region CreateNote Tests

        [Fact]
        public async Task CreateNote_WithValidNoteAndUser_CreatesNoteAndRelationship()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var noteServer = new NoteServer
            {
                IdNote = Guid.NewGuid(),
                Title = "New Note",
                Content = "Content",
                CreationDate = DateTime.UtcNow.ToString("O"),
                LastUpdate = DateTime.UtcNow.ToString("O"),
                Version = 1
            };

            // Act
            var result = await _repository.CreateNote(noteServer, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(noteServer.Content, result.Content);

            var noteInDb = await _dbContext.Notes.FirstOrDefaultAsync(n => n.IdNote == result.IdNote);
            Assert.NotNull(noteInDb);

            var relationship = await _dbContext.Note_Users.FirstOrDefaultAsync(
                nu => nu.IdNote == result.IdNote && nu.IdUser == userId
            );
            Assert.NotNull(relationship);
        }

        #endregion

        #region GetChangesFromNote Tests

        [Fact]
        public void GetChangesFromNote_WithValidNoteId_ReturnsChanges()
        {
            // Arrange
            var noteId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();

            var syncQueueItem = new SyncQueueServer
            {
                IdNote = noteId,
                IdUser = userId,
                IdDevice = deviceId,
                Operation = "Update",
                ContentChanges = "Content",
                LastUpdate = DateTime.UtcNow.ToString("O")
            };

            _dbContext.Sync_Queues.Add(syncQueueItem);
            _dbContext.SaveChanges();

            // Act
            var result = _repository.GetChangesFromNote(noteId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(noteId, result[0].IdNote);
        }

        #endregion

        #region SyncData Tests

        //[Fact]
        //public async Task SyncData_WithUpdateOperation_UpdatesNoteContent()
        //{
        //    // Arrange
        //    var noteId = Guid.NewGuid();
        //    var originalContent = "Original Content";
        //    var updatedContent = "Updated Content";

        //    var note = new NoteServer
        //    {
        //        IdNote = noteId,
        //        Title = "Test",
        //        Content = originalContent,
        //        CreationDate = DateTime.UtcNow.ToString("O"),
        //        LastUpdate = DateTime.UtcNow.ToString("O"),
        //        Version = 1
        //    };

        //    _dbContext.Notes.Add(note);
        //    await _dbContext.SaveChangesAsync();

        //    var syncChange = new SyncQueueServer
        //    {
        //        IdNote = noteId,
        //        Operation = "Update",
        //        ContentChanges = updatedContent,
        //        LastUpdate = DateTime.UtcNow.ToString("O"),
        //        IdUser = Guid.NewGuid(),
        //        IdDevice = Guid.NewGuid()
        //    };

        //    // Act
        //    await _repository.SyncData(syncChange);

        //    // Assert
        //    var updatedNote = await _dbContext.Notes.FirstOrDefaultAsync(n => n.IdNote == noteId);
        //    Assert.NotNull(updatedNote);
        //    Assert.Equal(updatedContent, updatedNote.Content);
        //}

        //[Fact]
        //public async Task SyncData_WithDeleteOperation_RemovesNote()
        //{
        //    // Arrange
        //    var noteId = Guid.NewGuid();
        //    var note = new NoteServer
        //    {
        //        IdNote = noteId,
        //        Title = "Test",
        //        Content = "Content to Delete",
        //        CreationDate = DateTime.UtcNow.ToString("O"),
        //        LastUpdate = DateTime.UtcNow.ToString("O"),
        //        Version = 1
        //    };

        //    _dbContext.Notes.Add(note);
        //    await _dbContext.SaveChangesAsync();

        //    var syncChange = new SyncQueueServer
        //    {
        //        IdNote = noteId,
        //        Operation = "Delete",
        //        ContentChanges = null,
        //        LastUpdate = DateTime.UtcNow.ToString("O"),
        //        IdUser = Guid.NewGuid(),
        //        IdDevice = Guid.NewGuid()
        //    };

        //    // Act
        //    await _repository.SyncData(syncChange);

        //    // Assert
        //    var deletedNote = await _dbContext.Notes.FirstOrDefaultAsync(n => n.IdNote == noteId);
        //    Assert.Null(deletedNote);
        //}

        #endregion
    }
}
