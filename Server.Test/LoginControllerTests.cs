using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Server.Controllers;
using Server.Test.ControllersTest;
using Xunit;

namespace Server.Tests
{
    public class LoginControllerTests : TestWebApplicationFactory<Program>
    {
        private DbContextServer CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new DbContextServer(options);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsUserAndNotes()
        {
            // Arrange
            using var context = CreateInMemoryContext();

            var user = new User
            {
                IdUser = 1,
                Name = "Test User",
                Username = "testuser",
                Password = "testpass"
            };

            var note = new ServerNote
            {
                IdNote = 1,
                Title = "Test Note",
                Content = "Test Content",
                StartingDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                lastUpdate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            var serverNoteUser = new ServerNoteUser
            {
                IdUser = user.IdUser,
                IdServerNote = note.IdNote,
                ServerNotes = note,
                User = user
            };

            user.ServerNotesUsers = new List<ServerNoteUser> { serverNoteUser };

            context.Users.Add(user);
            context.ServerNotes.Add(note);
            await context.SaveChangesAsync();

            var controller = new LoginController(context);

            // Act
            var result = await controller.Login("testuser", "testpass");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var jsonToken = JToken.FromObject(jsonResult.Value);
            Assert.True((bool)jsonToken["success"]);
            Assert.Equal("testuser", (string)jsonToken["user"]["Username"]);
            Assert.NotEmpty(jsonToken["notes"]);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsError()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var controller = new LoginController(context);

            // Act
            var result = await controller.Login("wronguser", "wrongpass");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var jsonToken = JToken.FromObject(jsonResult.Value);
            Assert.False((bool)jsonToken["success"]);
            Assert.Equal("Invalid username or password.", (string)jsonToken["message"]);
        }
    }
}