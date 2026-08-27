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
}
