using System.Net;
using FluentAssertions;
using KitCLI.Services;

namespace KitCLI.Tests.Services;

[Collection("Console Output Tests")]
public class ErrorHandlerTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Authentication failed")]
    [InlineData(HttpStatusCode.Forbidden, "Access denied")]
    [InlineData(HttpStatusCode.NotFound, "not found")]
    [InlineData(HttpStatusCode.TooManyRequests, "Rate limit")]
    [InlineData(HttpStatusCode.InternalServerError, "server error")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "temporarily unavailable")]
    public void HandleApiError_Should_Display_Appropriate_Message(HttpStatusCode statusCode, string expectedMessage)
    {
        // Arrange
        var originalError = Console.Error;
        try
        {
            var writer = new StringWriter();
            Console.SetError(writer);

            // Act
            ErrorHandler.HandleApiError(statusCode);

            // Assert
            var output = writer.ToString();
            output.ToLower().Should().Contain(expectedMessage.ToLower());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void HandleException_Should_Handle_HttpRequestException()
    {
        // Arrange
        var originalError = Console.Error;
        try
        {
            var writer = new StringWriter();
            Console.SetError(writer);
            var ex = new HttpRequestException("Connection failed");

            // Act
            ErrorHandler.HandleException(ex);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("connection");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void HandleException_Should_Handle_TaskCanceledException()
    {
        // Arrange
        var originalError = Console.Error;
        try
        {
            var writer = new StringWriter();
            Console.SetError(writer);
            var ex = new TaskCanceledException();

            // Act
            ErrorHandler.HandleException(ex);

            // Assert
            var output = writer.ToString();
            output.Should().Match(s => s.Contains("cancelled") || s.Contains("timed out"));
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProvideSuggestions_Should_Give_Helpful_Tips_For_Unauthorized()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            ErrorHandler.ProvideSuggestions(HttpStatusCode.Unauthorized);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("kit config set");
            output.Should().Contain("API key");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ProvideSuggestions_Should_Give_Tips_For_RateLimit()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            ErrorHandler.ProvideSuggestions(HttpStatusCode.TooManyRequests);

            // Assert
            var output = writer.ToString();
            output.Should().Match(s => s.Contains("Wait") || s.Contains("wait"));
            output.Should().Contain("--limit");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

