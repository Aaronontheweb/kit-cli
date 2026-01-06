using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;
using KitCLI.Tests.TestData.Builders;

namespace KitCLI.Tests.Commands;

/// <summary>
/// Tests for the tag commands.
/// Uses Console.SetOut for capturing output, so must not run in parallel with other console tests.
/// </summary>
[Collection("Console Output Tests")]
public class TagCommandsTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public TagCommandsTests()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    [Fact]
    public async Task HandleSubscribers_Should_Return_Subscribers_For_Tag()
    {
        // Arrange - Issue #76: verify tag subscribers returns actual subscribers
        var subscribers = new[]
        {
            new Subscriber { Id = 1, EmailAddress = "user1@test.com", State = "active" },
            new Subscriber { Id = 2, EmailAddress = "user2@test.com", State = "active" },
            new Subscriber { Id = 3, EmailAddress = "user3@test.com", State = "active" }
        };

        var mockClient = new MockKitApiClient
        {
            GetTagSubscribersAsyncFunc = (tagId, perPage, after, ct) =>
            {
                return Task.FromResult(new PaginatedResponse<Subscriber>
                {
                    Data = subscribers,
                    Pagination = new PaginationInfo { HasNextPage = false }
                });
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleSubscribers(["123"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("3 subscribers");
        output.Should().Contain("user1@test.com");
        output.Should().Contain("user2@test.com");
        output.Should().Contain("user3@test.com");
    }

    [Fact]
    public async Task HandleSubscribers_Should_Show_No_Subscribers_Message_When_Empty()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            GetTagSubscribersAsyncFunc = (tagId, perPage, after, ct) =>
            {
                return Task.FromResult(new PaginatedResponse<Subscriber>
                {
                    Data = [],
                    Pagination = new PaginationInfo { HasNextPage = false }
                });
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleSubscribers(["456"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("0 subscribers");
    }

    [Fact]
    public async Task HandleSubscribers_Should_Return_Error_For_Invalid_Id()
    {
        // Arrange
        var mockClient = new MockKitApiClient();

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleSubscribers(["not-a-number"], mockClient);

        // Assert
        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("Invalid tag ID");
    }

    [Fact]
    public async Task HandleSubscribers_Should_Show_Usage_With_No_Args()
    {
        // Arrange
        var mockClient = new MockKitApiClient();

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleSubscribers([], mockClient);

        // Assert
        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("Usage:");
        output.Should().Contain("kit tag subscribers");
    }

    [Fact]
    public async Task HandleList_Should_Return_Tags()
    {
        // Arrange
        var tags = new[]
        {
            new Tag { Id = 1, Name = "Newsletter" },
            new Tag { Id = 2, Name = "Blog Subscribers" },
            new Tag { Id = 3, Name = "Premium" }
        };

        var mockClient = new MockKitApiClient { Tags = tags.ToList() };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleList([], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("3 tags");
        output.Should().Contain("Newsletter");
        output.Should().Contain("Blog Subscribers");
        output.Should().Contain("Premium");
    }
}
