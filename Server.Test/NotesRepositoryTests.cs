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
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1
            };

            var note2 = new NoteServer
            {
                IdNote = noteId2,
                Title = "Note 2",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
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
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1
            };

            var note2 = new NoteServer
            {
                IdNote = noteId2,
                Title = "User 2 Note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
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
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await _repository.CreateNote(noteServer, userId);

            // Assert
            Assert.NotNull(result);

            var noteInDb = await _dbContext.Notes.FirstOrDefaultAsync(n => n.IdNote == result.IdNote);
            Assert.NotNull(noteInDb);

            var relationship = await _dbContext.Note_Users.FirstOrDefaultAsync(
                nu => nu.IdNote == result.IdNote && nu.IdUser == userId
            );
            Assert.NotNull(relationship);
        }

        #endregion

    }
}
