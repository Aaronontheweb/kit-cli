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
        // Arrange - Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var responseData = new SubscribersResponse
        {
            Subscribers = new[]
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

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SubscribersResponse);

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
        // Arrange - Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var page1 = new SubscribersResponse
        {
            Subscribers = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "test1@example.com", State = "active" },
                new Subscriber { Id = 2, EmailAddress = "test2@example.com", State = "active" }
            },
            Pagination = new PaginationInfo { HasNextPage = true, EndCursor = "cursor1" }
        };

        var page2 = new SubscribersResponse
        {
            Subscribers = new[]
            {
                new Subscriber { Id = 3, EmailAddress = "test3@example.com", State = "active" }
            },
            Pagination = new PaginationInfo { HasNextPage = false }
        };

        var json1 = JsonSerializer.Serialize(page1, KitJsonContext.Default.SubscribersResponse);
        var json2 = JsonSerializer.Serialize(page2, KitJsonContext.Default.SubscribersResponse);

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

    /// <summary>
    /// Regression test for #113: Cancelled subscriber list returns 0 results.
    /// The state parameter must be passed to the API as 'status' query parameter.
    /// </summary>
    [Fact]
    public async Task GetSubscribersAsync_Should_Pass_State_Parameter_To_Api()
    {
        // Arrange
        string? capturedUrl = null;
        var responseData = new SubscribersResponse
        {
            Subscribers = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "cancelled@example.com", State = "cancelled" }
            },
            Pagination = new PaginationInfo { HasNextPage = false }
        };

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SubscribersResponse);

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                capturedUrl = request.RequestUri!.PathAndQuery;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        // Act
        var result = await _client.GetSubscribersAsync(50, null, "cancelled");

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("status=cancelled");
        result.Data.Should().HaveCount(1);
        result.Data[0].State.Should().Be("cancelled");
    }

    /// <summary>
    /// Regression test for #113: GetAllSubscribersAsync should pass state to API.
    /// </summary>
    [Fact]
    public async Task GetAllSubscribersAsync_Should_Pass_State_To_Api()
    {
        // Arrange
        var capturedUrls = new List<string>();
        var responseData = new SubscribersResponse
        {
            Subscribers = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "cancelled@example.com", State = "cancelled" }
            },
            Pagination = new PaginationInfo { HasNextPage = false }
        };

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SubscribersResponse);

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                capturedUrls.Add(request.RequestUri!.PathAndQuery);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        // Act
        var subscribers = new List<Subscriber>();
        await foreach (var subscriber in _client.GetAllSubscribersAsync("cancelled"))
        {
            subscribers.Add(subscriber);
        }

        // Assert - verify the state was passed to the API
        capturedUrls.Should().HaveCountGreaterThan(0);
        capturedUrls[0].Should().Contain("status=cancelled");
        subscribers.Should().HaveCount(1);
    }

    /// <summary>
    /// Regression test for #112: Form cohort returns 0 subscribers.
    /// GetFormSubscribersAsync must use SubscribersResponse type to properly parse the API response.
    /// </summary>
    [Fact]
    public async Task GetFormSubscribersAsync_Should_Parse_Subscribers_Correctly()
    {
        // Arrange - Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var responseData = new SubscribersResponse
        {
            Subscribers = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "form1@example.com", State = "active" },
                new Subscriber { Id = 2, EmailAddress = "form2@example.com", State = "active" },
                new Subscriber { Id = 3, EmailAddress = "form3@example.com", State = "cancelled" }
            },
            Pagination = new PaginationInfo
            {
                HasNextPage = true,
                EndCursor = "cursor123"
            }
        };

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SubscribersResponse);

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
        var result = await _client.GetFormSubscribersAsync(12345, 50);

        // Assert - this was returning 0 in #112 due to incorrect response type
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(3);
        result.Data[0].EmailAddress.Should().Be("form1@example.com");
        result.Data[2].State.Should().Be("cancelled");
        result.Pagination!.HasNextPage.Should().BeTrue();
        result.Pagination.EndCursor.Should().Be("cursor123");
    }

    /// <summary>
    /// Regression test for #112: Segment subscribers also needs correct response parsing.
    /// </summary>
    [Fact]
    public async Task GetSegmentSubscribersAsync_Should_Parse_Subscribers_Correctly()
    {
        // Arrange - Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var responseData = new SubscribersResponse
        {
            Subscribers = new[]
            {
                new Subscriber { Id = 1, EmailAddress = "seg1@example.com", State = "active" },
                new Subscriber { Id = 2, EmailAddress = "seg2@example.com", State = "active" }
            },
            Pagination = new PaginationInfo
            {
                HasNextPage = false
            }
        };

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SubscribersResponse);

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
        var result = await _client.GetSegmentSubscribersAsync(12345, 50);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data[0].EmailAddress.Should().Be("seg1@example.com");
        result.Pagination!.HasNextPage.Should().BeFalse();
    }
}
