using FluentAssertions;
using KitCLI.Services;
using Moq;
using Moq.Protected;
using System.Net;

namespace KitCLI.Tests.Services;

public class RateLimitHandlerTests
{
    [Fact]
    public async Task Should_Retry_On_RateLimit_With_Exponential_Backoff()
    {
        // Arrange
        var innerHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;
        
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(100)) }
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        
        var rateLimitHandler = new RateLimitHandler(innerHandler.Object);
        var httpClient = new HttpClient(rateLimitHandler);
        
        // Act
        var response = await httpClient.GetAsync("https://api.example.com/test");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(3);
    }
    
    [Fact]
    public async Task Should_Respect_RetryAfter_Header()
    {
        // Arrange
        var innerHandler = new Mock<HttpMessageHandler>();
        var retryAfterSeconds = 1;
        var startTime = DateTime.UtcNow;
        
        innerHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds)) }
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        
        var rateLimitHandler = new RateLimitHandler(innerHandler.Object);
        var httpClient = new HttpClient(rateLimitHandler);
        
        // Act
        var response = await httpClient.GetAsync("https://api.example.com/test");
        var elapsed = DateTime.UtcNow - startTime;
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        elapsed.TotalSeconds.Should().BeGreaterThanOrEqualTo(retryAfterSeconds - 0.1); // Allow small margin
    }
    
    [Fact]
    public async Task Should_Stop_After_Max_Retries()
    {
        // Arrange
        var innerHandler = new Mock<HttpMessageHandler>();
        
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        
        var rateLimitHandler = new RateLimitHandler(innerHandler.Object);
        var httpClient = new HttpClient(rateLimitHandler);
        
        // Act
        var response = await httpClient.GetAsync("https://api.example.com/test");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        
        // Verify it was called exactly MaxRetries + 1 times (initial + retries)
        innerHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(4), // 1 initial + 3 retries
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
    
    [Fact]
    public async Task Should_Not_Retry_On_Success()
    {
        // Arrange
        var innerHandler = new Mock<HttpMessageHandler>();
        
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        
        var rateLimitHandler = new RateLimitHandler(innerHandler.Object);
        var httpClient = new HttpClient(rateLimitHandler);
        
        // Act
        var response = await httpClient.GetAsync("https://api.example.com/test");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        innerHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
    
    [Fact]
    public async Task Should_Not_Retry_On_Client_Errors()
    {
        // Arrange
        var innerHandler = new Mock<HttpMessageHandler>();
        
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));
        
        var rateLimitHandler = new RateLimitHandler(innerHandler.Object);
        var httpClient = new HttpClient(rateLimitHandler);
        
        // Act
        var response = await httpClient.GetAsync("https://api.example.com/test");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        innerHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}