using FluentAssertions;
using KitCLI.Models;
using KitCLI.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KitCLI.Tests.Services;

public class SequenceEmailApiTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly KitConfig _config;
    private readonly KitApiClient _client;

    public SequenceEmailApiTests()
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
    public async Task GetSequenceEmailsAsync_Should_Return_Emails_And_Honor_IncludeContent()
    {
        // Arrange - Kit V4 API returns {"emails": [...], "pagination": {...}}
        var responseData = new SequenceEmailsResponse
        {
            Emails = new[]
            {
                new SequenceEmail { Id = 1, SequenceId = 42, Subject = "Welcome", Position = 1, DelayValue = 3, DelayUnit = "days", EmailAddress = "team@example.com" },
                new SequenceEmail { Id = 2, SequenceId = 42, Subject = "Follow up", Position = 2, DelayValue = 5, DelayUnit = "hours", EmailAddress = "team@example.com" }
            },
            Pagination = new PaginationInfo { HasNextPage = false }
        };

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SequenceEmailsResponse);

        Uri? requestUri = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                requestUri = request.RequestUri;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        // Act
        var result = await _client.GetSequenceEmailsAsync(42, includeContent: true, includeStats: true);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data[0].Subject.Should().Be("Welcome");
        result.Data[1].DelayFormatted.Should().Be("5h");
        requestUri.Should().NotBeNull();
        requestUri!.PathAndQuery.Should().Contain("/sequences/42/emails");
        requestUri.Query.Should().Contain("include_content=true");
        requestUri.Query.Should().Contain("include=stats");
    }

    [Fact]
    public async Task GetSequenceEmailsAsync_Should_Throw_On_404()
    {
        // Arrange - bad sequence ID should surface as an error, not a silent empty list
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
        var act = async () => await _client.GetSequenceEmailsAsync(999);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetSequenceAsync_Should_Use_Direct_Endpoint_And_Deserialize_Wrapper()
    {
        Uri? requestUri = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                requestUri = request.RequestUri;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""{"sequence":{"id":42,"name":"Welcome","email_count":2}}""", Encoding.UTF8, "application/json")
                };
            });

        var sequence = await _client.GetSequenceAsync(42);

        sequence.Should().NotBeNull();
        sequence!.Id.Should().Be(42);
        sequence.Name.Should().Be("Welcome");
        requestUri!.PathAndQuery.Should().Be("/v4/sequences/42");
    }

    [Fact]
    public async Task GetSequenceAsync_Should_Return_Null_When_NotFound()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

        var sequence = await _client.GetSequenceAsync(999);

        sequence.Should().BeNull();
    }

    [Fact]
    public async Task GetSequenceSubscribersAsync_Should_Use_Status_Query_And_Deserialize_Subscribers_Envelope()
    {
        Uri? requestUri = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                requestUri = request.RequestUri;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""
                        {"subscribers":[{"id":923,"email_address":"subscriber@kit.dev","state":"completed"}],"pagination":{"has_next_page":false}}
                        """, Encoding.UTF8, "application/json")
                };
            });

        var response = await _client.GetSequenceSubscribersAsync(42, "all");

        response.Data.Should().ContainSingle();
        response.Data[0].EmailAddress.Should().Be("subscriber@kit.dev");
        response.Data[0].IsCompleted.Should().BeTrue();
        requestUri!.Query.Should().Contain("status=all");
        requestUri.Query.Should().NotContain("state=");
    }

    [Fact]
    public async Task GetSequenceEmailAsync_Should_Return_Email()
    {
        // Arrange - Kit V4 API returns {"email": {...}}
        var responseData = new SequenceEmailResponse
        {
            Email = new SequenceEmail
            {
                Id = 5,
                SequenceId = 42,
                Subject = "Welcome email",
                EmailAddress = "team@example.com",
                EmailTemplateId = 77,
                Published = true,
                Position = 1,
                DelayValue = 3,
                DelayUnit = "days",
                SendDays = new[] { "monday" },
                Content = "<p>Hello</p>"
            }
        };

        var json = JsonSerializer.Serialize(responseData, KitJsonContext.Default.SequenceEmailResponse);

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
        var result = await _client.GetSequenceEmailAsync(42, 5);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Subject.Should().Be("Welcome email");
        result.EmailAddress.Should().Be("team@example.com");
        result.EmailTemplateId.Should().Be(77);
        result.Published.Should().BeTrue();
        result.DelayFormatted.Should().Be("3d");
        result.SendDays.Should().Contain("monday");
        result.Content.Should().Be("<p>Hello</p>");
    }

    [Fact]
    public async Task GetSequenceEmailAsync_Should_Return_Null_When_NotFound()
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
        var result = await _client.GetSequenceEmailAsync(999, 123);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSequenceStatsAsync_Should_Request_All_Email_Pages_And_Aggregate_NonZero_Rates()
    {
        // Arrange - branch responses by endpoint: sequences list, emails (with stats), subscribers
        Uri? emailsRequestUri = null;
        Uri? subscribersRequestUri = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                var path = request.RequestUri!.AbsolutePath;
                string json;
                if (path.Contains("/emails"))
                {
                    emailsRequestUri = request.RequestUri;
                    json = request.RequestUri!.Query.Contains("after=next-page")
                        ? """
                            {"emails":[{"id":2,"sequence_id":42,"subject":"Follow up","stats":{"recipients":500,"opens":250,"clicks":75,"open_rate":50.0,"click_rate":15.0}}],"pagination":{"has_next_page":false}}
                            """
                        : """
                            {"emails":[{"id":1,"sequence_id":42,"subject":"Welcome","stats":{"recipients":1000,"opens":400,"clicks":100,"open_rate":40.0,"click_rate":10.0}}],"pagination":{"has_next_page":true,"end_cursor":"next-page"}}
                            """;
                }
                else if (path.Contains("/subscribers"))
                {
                    subscribersRequestUri = request.RequestUri;
                    json = """{"subscribers":[{"id":1,"state":"active"}],"pagination":{"has_next_page":false}}""";
                }
                else
                {
                    json = """{"sequence":{"id":42,"name":"Welcome","subscriber_count":100}}""";
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        // Act
        var stats = await _client.GetSequenceStatsAsync(42);

        // Assert
        stats.Should().NotBeNull();
        stats!.SequenceId.Should().Be(42);
        stats.TotalSubscribers.Should().Be(100);
        stats.AverageOpenRate.Should().BeApproximately(45.0, 0.001);
        stats.AverageClickRate.Should().BeApproximately(12.5, 0.001);
        stats.EmailsSent.Should().Be(1500);
        emailsRequestUri.Should().NotBeNull();
        emailsRequestUri!.Query.Should().Contain("include=stats");
        subscribersRequestUri!.Query.Should().Contain("status=all");
    }
}
