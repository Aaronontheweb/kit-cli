using FluentAssertions;
using KitCLI.Models;
using KitCLI.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KitCLI.Tests.Services;

public class KitApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly KitConfig _config;
    private readonly KitApiClient _client;
    
    public KitApiClientTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.kit.com/v4/")
        };
        
        _config = new KitConfig
        {
            ApiKey = "test-api-key",
            ApiVersion = "v4"
        };
        
        _client = new KitApiClient(_config, _httpClient);
    }
    
    [Fact]
    public async Task GetSubscribersAsync_Should_Return_Paginated_Subscribers()
    {
        // Arrange
        var responseData = new PaginatedResponse<Subscriber>
        {
            Data = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "test1@example.com" },
                new Subscriber { Id = 2, EmailAddress = "test2@example.com" }
            },
            Pagination = new PaginationInfo
            {
                HasNextPage = true,
                EndCursor = "cursor123"
            }
        };
        
        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.PaginatedResponseSubscriber);
        
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        
        // Act
        var result = await _client.GetSubscribersAsync(50);
        
        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data[0].EmailAddress.Should().Be("test1@example.com");
        result.Pagination!.HasNextPage.Should().BeTrue();
        result.Pagination.EndCursor.Should().Be("cursor123");
    }
    
    [Fact]
    public async Task GetSubscriberAsync_Should_Return_Null_When_NotFound()
    {
        // Arrange
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });
        
        // Act
        var result = await _client.GetSubscriberAsync(999);
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetAllSubscribersAsync_Should_Stream_All_Pages()
    {
        // Arrange
        var page1 = new PaginatedResponse<Subscriber>
        {
            Data = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "test1@example.com", State = "active" },
                new Subscriber { Id = 2, EmailAddress = "test2@example.com", State = "active" }
            },
            Pagination = new PaginationInfo { HasNextPage = true, EndCursor = "cursor1" }
        };
        
        var page2 = new PaginatedResponse<Subscriber>
        {
            Data = new[]
            {
                new Subscriber { Id = 3, EmailAddress = "test3@example.com", State = "active" }
            },
            Pagination = new PaginationInfo { HasNextPage = false }
        };
        
        var json1 = JsonSerializer.Serialize(page1, KitJsonContext.Default.PaginatedResponseSubscriber);
        var json2 = JsonSerializer.Serialize(page2, KitJsonContext.Default.PaginatedResponseSubscriber);
        
        var callCount = 0;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(callCount == 1 ? json1 : json2, Encoding.UTF8, "application/json")
                };
            });
        
        // Act
        var subscribers = new List<Subscriber>();
        await foreach (var subscriber in _client.GetAllSubscribersAsync("active"))
        {
            subscribers.Add(subscriber);
        }
        
        // Assert
        subscribers.Should().HaveCount(3);
        subscribers[0].Id.Should().Be(1);
        subscribers[1].Id.Should().Be(2);
        subscribers[2].Id.Should().Be(3);
        callCount.Should().Be(2);
    }
    
    [Fact]
    public async Task TestConnectionAsync_Should_Return_True_When_Successful()
    {
        // Arrange
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/account")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });
        
        // Act
        var result = await _client.TestConnectionAsync();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task TestConnectionAsync_Should_Try_Subscribers_When_Account_NotFound()
    {
        // Arrange
        var accountCalled = false;
        var subscribersCalled = false;
        
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.RequestUri!.PathAndQuery.Contains("/account"))
                {
                    accountCalled = true;
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
                }
                if (request.RequestUri.PathAndQuery.Contains("/subscribers"))
                {
                    subscribersCalled = true;
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
                }
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest };
            });
        
        // Act
        var result = await _client.TestConnectionAsync();
        
        // Assert
        result.Should().BeTrue();
        accountCalled.Should().BeTrue();
        subscribersCalled.Should().BeTrue();
    }
}