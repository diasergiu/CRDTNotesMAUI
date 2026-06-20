using System.Net;
using System.Net.Http.Json;
using DatabaseLibrary.Services;
using DatabaseLibrary.WrapperClasses;
using Moq;
using Moq.Protected;
using Xunit;

namespace CRDT_TestShering.Test.ServiceTest
{
    public class LoginServiceTest
    {
        [Fact]
        public async Task LoginAsync_SuccessfulLogin_ReturnsSuccessWithNotes()
        {
            // Arrange
            var expectedNotes = new List<NoteInfo>
            {
                new NoteInfo { IdNote = 1, Title = "Test Note" }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(expectedNotes)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var service = new LoginConnectionServices(httpClient.ToString()); // Need to modify constructor

            // Act
            var result = await service.LoginAsync("testuser", "testpass");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal("Test Note", result.Data[0].Title);
        }

        [Fact]
        public async Task LoginAsync_ServerError_ReturnsFailure()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Server error")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var service = new LoginConnectionServices(httpClient.ToString());

            // Act
            var result = await service.LoginAsync("testuser", "testpass");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ApiErrorType.ServerError, result.ErrorType);
        }

        [Fact]
        public async Task LoginAsync_Timeout_ReturnsTimeoutError()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new TaskCanceledException());

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var service = new LoginConnectionServices(httpClient.ToString());

            // Act
            var result = await service.LoginAsync("testuser", "testpass");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ApiErrorType.Timeout, result.ErrorType);
        }
    }
}
