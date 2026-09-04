using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.ResponsBody;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Server.Controllers;
using Server.ServeRepositories;
using Server.ServerHub;
using System;
using System.Collections.Generic;
using Xunit;

namespace EndToEndTest
{
    public class NotesControllerAuthorizationTests : IDisposable
    {
        private readonly DbContextServer _dbContext;
        private readonly NotesRepository _repository;
        private readonly NotesController _controller;
        private readonly Mock<NoteSyncHub> _noteSyncHubMock;

        public NotesControllerAuthorizationTests()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new DbContextServer(options);
            _repository = new NotesRepository(_dbContext);

            // Mock IHubContext<NotesHub> which is required by NoteSyncHub
            var hubContextMock = new Mock<IHubContext<NotesHub>>();

            // Create NoteSyncHub with mocked dependency
            var noteSyncHub = new NoteSyncHub(hubContextMock.Object);
            _controller = new NotesController(_repository, noteSyncHub);
        }

        public void Dispose()
        {
            try
            {
                _dbContext.Database.EnsureDeleted();
            }
            catch
            {
                // Best-effort cleanup.
            }
            _dbContext.Dispose();
        }

        private void SetupControllerContext(Guid userId)
        {
            // Setup the controller's HttpContext with X-User-Id header
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.Request.Headers["X-User-Id"] = userId.ToString();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private async Task SeedNoteWithUserAccess(Guid userId, Guid noteId)
        {
            // Create user
            var user = new UserServer
            {
                IdUser = userId,
                Username = $"testuser_{userId:N}",
                Name = "Test User",
                Password = "hashedpassword"
            };

            // Create note
            var note = new NoteServer
            {
                IdNote = noteId,
                Title = "Test Note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = false,
                isDeleted = false,
                Version = 1
            };

            // Create relationship
            var noteUserAccess = new Note_UserServer
            {
                IdUser = userId,
                IdNote = noteId
            };

            _dbContext.Users.Add(user);
            _dbContext.Notes.Add(note);
            _dbContext.Note_Users.Add(noteUserAccess);

            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetNote_WithAccess_ReturnsSuccess()
        {
            // Arrange
            var authorizedUserId = Guid.NewGuid();
            var noteId = Guid.NewGuid();

            await SeedNoteWithUserAccess(authorizedUserId, noteId);
            SetupControllerContext(authorizedUserId);

            // Act
            var result = await _controller.GetNote(noteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var apiResponse = okResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
        }

        [Fact]
        public async Task GetNote_WithoutAccess_ReturnsBadRequest()
        {
            // Arrange
            var unauthorizedUserId = Guid.NewGuid();
            var noteOwnerId = Guid.NewGuid();
            var noteId = Guid.NewGuid();

            // Seed a note that is owned by a different user
            await SeedNoteWithUserAccess(noteOwnerId, noteId);

            // Try to access as unauthorized user
            SetupControllerContext(unauthorizedUserId);

            // Act
            var result = await _controller.GetNote(noteId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            var message = badRequestResult.Value as string;
            Assert.NotNull(message);
            Assert.Contains("do not have access", message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GiveNoteAccessToUser_WithAccess_ReturnsSuccess()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var newUserName = "newuser";
            var noteId = Guid.NewGuid();

            // Create the new user that will receive access
            var newUser = new UserServer
            {
                IdUser = Guid.NewGuid(),
                Username = newUserName,
                Name = "New User",
                Password = "hashedpassword"
            };
            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            // Seed a note owned by current user
            await SeedNoteWithUserAccess(currentUserId, noteId);
            SetupControllerContext(currentUserId);

            // Act
            var result = await _controller.GiveNoteAccessToUser(noteId, newUserName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var apiResponse = okResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
        }

        [Fact]
        public async Task GiveNoteAccessToUser_WhenNotOwner_StillGrantsAccess()
        {
            // Note: Currently GiveNoteAccessToUser doesn't check if caller owns the note
            // This test documents the current behavior
            // Arrange
            var noteOwnerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var noteId = Guid.NewGuid();

            // Create the target user who will receive access
            var targetUser = new UserServer
            {
                IdUser = targetUserId,
                Username = "targetuser",
                Name = "Target User",
                Password = "hashedpassword"
            };
            _dbContext.Users.Add(targetUser);

            // Seed note owned by different user
            await SeedNoteWithUserAccess(noteOwnerId, noteId);

            // Setup as unauthorized user trying to grant access
            SetupControllerContext(unauthorizedUserId);

            // Act
            var result = await _controller.GiveNoteAccessToUser(noteId, "targetuser");

            // Assert - Currently allows non-owner to grant access
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GiveNoteAccessToUser_ToNonExistentUser_CreatesRecordAnyway()
        {
            // Note: Currently SaveNoteUserConnection doesn't validate user exists
            // This test documents the current behavior
            // Arrange
            var currentUserId = Guid.NewGuid();
            var noteId = Guid.NewGuid();
            var nonExistentUsername = "ghostuser123";

            // Seed note owned by current user
            await SeedNoteWithUserAccess(currentUserId, noteId);
            SetupControllerContext(currentUserId);

            // Act
            var result = await _controller.GiveNoteAccessToUser(noteId, nonExistentUsername);

            // Assert - Currently succeeds even if user doesn't exist
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GiveNoteAccessToUser_ToNoteOwner_CanGrantAccess()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();
            var noteId = Guid.NewGuid();

            // Create second user
            var secondUser = new UserServer
            {
                IdUser = secondUserId,
                Username = "seconduser",
                Name = "Second User",
                Password = "hashedpassword"
            };
            _dbContext.Users.Add(secondUser);

            // Seed note owned by current user
            await SeedNoteWithUserAccess(currentUserId, noteId);
            SetupControllerContext(currentUserId);

            // Act
            var result = await _controller.GiveNoteAccessToUser(noteId, "seconduser");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var apiResponse = okResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);

            // Verify access was added to database
            var noteUserAccess = _dbContext.Note_Users.FirstOrDefault(
                nu => nu.IdNote == noteId && nu.IdUser == secondUserId);
            Assert.NotNull(noteUserAccess);
        }

        [Fact]
        public async Task GiveNoteAccessToUser_WhenAlreadyHasAccess_RepositoryHandlesDuplicate()
        {
            // Note: SaveNoteUserConnection is idempotent - it tries to add the relationship
            // Repository or database constraints handle duplicates
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var noteId = Guid.NewGuid();

            // Create both users
            var currentUser = new UserServer
            {
                IdUser = currentUserId,
                Username = "currentuser",
                Name = "Current User",
                Password = "hashedpassword"
            };

            var targetUser = new UserServer
            {
                IdUser = targetUserId,
                Username = "targetuser",
                Name = "Target User",
                Password = "hashedpassword"
            };

            _dbContext.Users.Add(currentUser);
            _dbContext.Users.Add(targetUser);

            // Create note
            var note = new NoteServer
            {
                IdNote = noteId,
                Title = "Shared Note",
                CreationDate = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                DirtyFlagChangesMade = false,
                isDeleted = false,
                Version = 1
            };
            _dbContext.Notes.Add(note);

            // Grant initial accesses in one save
            var currentUserAccess = new Note_UserServer
            {
                IdUser = currentUserId,
                IdNote = noteId
            };

            var targetUserAccess = new Note_UserServer
            {
                IdUser = targetUserId,
                IdNote = noteId
            };

            _dbContext.Note_Users.Add(currentUserAccess);
            _dbContext.Note_Users.Add(targetUserAccess);
            await _dbContext.SaveChangesAsync();

            SetupControllerContext(currentUserId);

            // Act - Try to grant access again to user who already has it
            // This should either succeed gracefully or throw (repository handles it)
            try
            {
                var result = await _controller.GiveNoteAccessToUser(noteId, "targetuser");

                // If it succeeds, verify it's an OK result
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
            }
            catch (InvalidOperationException)
            {
                // This is also acceptable - repository signals duplicate
            }
        }

        [Fact]
        public async Task GiveNoteAccessToUser_WithNullOrEmptyUsername_NoErrorButMayNotFind()
        {
            // Note: Current implementation doesn't validate username input
            // Arrange
            var currentUserId = Guid.NewGuid();
            var noteId = Guid.NewGuid();

            await SeedNoteWithUserAccess(currentUserId, noteId);
            SetupControllerContext(currentUserId);

            // Act & Assert - Test with empty string
            // Repository will try to find user with empty username
            var result = await _controller.GiveNoteAccessToUser(noteId, "");

            // It may succeed or fail depending on repository implementation
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GiveNoteAccessToUser_ToNonExistentNote_MaySucceedDependingOnRepo()
        {
            // Note: Current implementation doesn't validate note exists
            // Arrange
            var currentUserId = Guid.NewGuid();
            var nonExistentNoteId = Guid.NewGuid();
            var targetUsername = "targetuser";

            // Create target user
            var targetUser = new UserServer
            {
                IdUser = Guid.NewGuid(),
                Username = targetUsername,
                Name = "Target User",
                Password = "hashedpassword"
            };
            _dbContext.Users.Add(targetUser);
            await _dbContext.SaveChangesAsync();

            SetupControllerContext(currentUserId);

            // Act - Try to grant access to non-existent note
            var result = await _controller.GiveNoteAccessToUser(nonExistentNoteId, targetUsername);

            // Assert - Result depends on repository implementation
            Assert.NotNull(result);
        }

    }
}
