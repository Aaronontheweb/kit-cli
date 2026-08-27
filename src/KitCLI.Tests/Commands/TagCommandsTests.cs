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

    [Fact]
    public async Task HandleCreate_Should_Create_Tag()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            CreateTagAsyncFunc = (request, ct) =>
                Task.FromResult<Tag?>(new Tag { Id = 99, Name = request.Name, CreatedAt = DateTime.UtcNow })
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleCreate(["VIP"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("VIP");
        output.Should().Contain("ID: 99");
    }

    [Fact]
    public async Task HandleCreate_Should_Show_Usage_With_No_Name()
    {
        // Arrange
        var mockClient = new MockKitApiClient();

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleCreate([], mockClient);

        // Assert
        result.Should().Be(1);
        writer.ToString().Should().Contain("Usage:");
        writer.ToString().Should().Contain("kit tag create");
    }

    [Fact]
    public async Task HandleRename_Should_Rename_Tag()
    {
        // Arrange
        Tag? renamed = null;
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 5, Name = "Old Name" } },
            RenameTagAsyncFunc = (id, name, ct) =>
            {
                renamed = new Tag { Id = id, Name = name };
                return Task.FromResult<Tag?>(renamed);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleRename(["5", "New Name"], mockClient);

        // Assert
        result.Should().Be(0);
        renamed!.Name.Should().Be("New Name");
        writer.ToString().Should().Contain("New Name");
    }

    [Fact]
    public async Task HandleRename_Should_Return_Error_For_Missing_Tag()
    {
        // Arrange
        var mockClient = new MockKitApiClient { Tags = new List<Tag>() };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        // Act
        var result = await TagCommands.HandleRename(["123", "New Name"], mockClient);

        // Assert
        result.Should().Be(1);
        writer.ToString().Should().Contain("Tag not found");
    }

    [Fact]
    public async Task HandleAddSubscriber_Should_Delegate_To_Subscriber_AddTag()
    {
        // Arrange
        bool tagged = false;
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } },
            Subscribers = new List<Subscriber> { new Subscriber { Id = 42, EmailAddress = "user@test.com" } },
            TagSubscriberAsyncFunc = (tagId, email, ct) =>
            {
                tagged = true;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await TagCommands.HandleAddSubscriber(["3", "user@test.com"], mockClient);

        // Assert
        result.Should().Be(0);
        tagged.Should().BeTrue();
        writer.ToString().Should().Contain("user@test.com");
    }

    [Fact]
    public async Task HandleAddSubscriber_Should_Use_Canonical_Subscriber_Validation()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        // Act
        var result = await TagCommands.HandleAddSubscriber(["3", "not-an-email"], mockClient);

        // Assert
        result.Should().Be(1);
        writer.ToString().Should().Contain("Invalid identifier");
    }

    [Fact]
    public async Task HandleAddSubscriber_Should_Forward_Canonical_Create_And_Tag_Options()
    {
        var created = new List<string>();
        var appliedTagIds = new List<long>();
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 2, Name = "Existing" } },
            Subscribers = new List<Subscriber> { new Subscriber { Id = 42, EmailAddress = "user@test.com" } },
            CreateTagAsyncFunc = (request, _) =>
            {
                created.Add(request.Name);
                return Task.FromResult<Tag?>(new Tag { Id = 9, Name = request.Name });
            },
            TagSubscriberAsyncFunc = (tagId, _, _) =>
            {
                appliedTagIds.Add(tagId);
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await TagCommands.HandleAddSubscriber(
            ["New Tag", "user@test.com", "--create", "--tag", "Existing"],
            mockClient);

        result.Should().Be(0);
        created.Should().Equal("New Tag");
        appliedTagIds.Should().Equal(9L, 2L);
    }

    [Fact]
    public async Task HandleAddSubscriber_Help_Should_Describe_Forwarded_Create_Option()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await TagCommands.HandleAddSubscriber(["--help"], new MockKitApiClient());

        result.Should().Be(0);
        writer.ToString().Should().Contain("--create");
    }

    [Fact]
    public async Task HandleRemoveSubscriber_Should_Delegate_To_Subscriber_RemoveTag()
    {
        // Arrange
        bool untagged = false;
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } },
            GetSubscriberAsyncFunc = (id, ct) =>
                Task.FromResult<Subscriber?>(new Subscriber { Id = 42, EmailAddress = "user@test.com" }),
            UntagSubscriberAsyncFunc = (tagId, subId, ct) =>
            {
                untagged = true;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        // Act
        var result = await TagCommands.HandleRemoveSubscriber(["3", "42", "--force"], mockClient);

        // Assert
        result.Should().Be(0);
        untagged.Should().BeTrue();
        writer.ToString().Should().Contain("Removed tag");
    }

    [Fact]
    public async Task HandleRemoveSubscriber_Should_Cancel_Without_Confirmation()
    {
        // Arrange
        bool untagged = false;
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } },
            GetSubscriberAsyncFunc = (id, ct) =>
                Task.FromResult<Subscriber?>(new Subscriber { Id = 42, EmailAddress = "user@test.com" }),
            UntagSubscriberAsyncFunc = (tagId, subId, ct) =>
            {
                untagged = true;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetIn(new StringReader("n"));

        // Act
        var result = await TagCommands.HandleRemoveSubscriber(["3", "42"], mockClient);

        // Assert
        result.Should().Be(0);
        untagged.Should().BeFalse();
        writer.ToString().Should().Contain("Cancelled");
    }

    [Theory]
    [InlineData("subscriber")]
    [InlineData("alias")]
    public async Task HandleRemoveTag_And_Alias_Should_Surface_Api_Failures(string command)
    {
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } },
            GetSubscriberAsyncFunc = (id, ct) =>
                Task.FromResult<Subscriber?>(new Subscriber { Id = 42, EmailAddress = "user@test.com" }),
            UntagSubscriberAsyncFunc = (_, _, _) =>
                throw new HttpRequestException("Invalid API key")
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        var result = command == "subscriber"
            ? await SubscriberCommands.HandleRemoveTag(["42", "--tag", "3", "--force"], mockClient)
            : await TagCommands.HandleRemoveSubscriber(["3", "42", "--force"], mockClient);

        result.Should().Be(1);
        writer.ToString().Should().Contain("Invalid API key");
    }

    [Fact]
    public async Task Tag_And_Subscriber_Aliases_Should_Prefer_A_Numeric_Name_Over_A_Numeric_Id()
    {
        var appliedTagIds = new List<long>();
        var removedTagIds = new List<long>();
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag>
            {
                new Tag { Id = 123, Name = "Different tag" },
                new Tag { Id = 456, Name = "123" }
            },
            Subscribers = new List<Subscriber> { new Subscriber { Id = 42, EmailAddress = "user@test.com" } },
            TagSubscriberAsyncFunc = (tagId, _, _) =>
            {
                appliedTagIds.Add(tagId);
                return Task.FromResult(true);
            },
            UntagSubscriberAsyncFunc = (tagId, _, _) =>
            {
                removedTagIds.Add(tagId);
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        (await SubscriberCommands.HandleAddTag(["user@test.com", "--tag", "123"], mockClient)).Should().Be(0);
        (await TagCommands.HandleAddSubscriber(["123", "user@test.com"], mockClient)).Should().Be(0);
        (await SubscriberCommands.HandleRemoveTag(["42", "--tag", "123", "--force"], mockClient)).Should().Be(0);
        (await TagCommands.HandleRemoveSubscriber(["123", "42", "--force"], mockClient)).Should().Be(0);

        appliedTagIds.Should().Equal(456L, 456L);
        removedTagIds.Should().Equal(456L, 456L);
    }

    [Theory]
    [InlineData("subscriber")]
    [InlineData("alias")]
    public async Task HandleAddTag_And_Alias_Should_Create_A_Missing_Numeric_Tag_Name(string command)
    {
        var createdNames = new List<string>();
        var appliedTagIds = new List<long>();
        var mockClient = new MockKitApiClient
        {
            Subscribers = new List<Subscriber> { new Subscriber { Id = 42, EmailAddress = "user@test.com" } },
            CreateTagAsyncFunc = (request, _) =>
            {
                createdNames.Add(request.Name);
                return Task.FromResult<Tag?>(new Tag { Id = 9, Name = request.Name });
            },
            TagSubscriberAsyncFunc = (tagId, _, _) =>
            {
                appliedTagIds.Add(tagId);
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        var result = command == "subscriber"
            ? await SubscriberCommands.HandleAddTag(["user@test.com", "--tag", "123", "--create"], mockClient)
            : await TagCommands.HandleAddSubscriber(["123", "user@test.com", "--create"], mockClient);

        result.Should().Be(0);
        createdNames.Should().Equal("123");
        appliedTagIds.Should().Equal(9L);
    }

    [Fact]
    public async Task HandleBulkCreate_Should_Return_1_When_Any_Record_Fails()
    {
        // Arrange - one record succeeds, one fails: per-record failure must surface as exit code 1
        var created = new List<string>();
        var mockClient = new MockKitApiClient
        {
            CreateTagAsyncFunc = (request, ct) =>
            {
                if (request.Name == "bad")
                {
                    throw new HttpRequestException("boom");
                }

                created.Add(request.Name);
                return Task.FromResult<Tag?>(new Tag { Id = created.Count, Name = request.Name });
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        // Act
        var result = await TagCommands.HandleBulkCreate(["good", "bad"], mockClient);

        // Assert
        result.Should().Be(1);
        created.Should().Contain("good");
        created.Should().NotContain("bad");
        var output = writer.ToString();
        output.Should().Contain("Preflight:");
        output.Should().Contain("Total: 2");
        output.Should().Contain("created: 1");
        output.Should().Contain("Failed: 1");
        output.Should().Contain("boom");
    }

    [Fact]
    public async Task HandleBulkRemove_Should_Ignore_Force_Flag_As_Data_Item()
    {
        // Arrange - regression: -y in the inline args must not be counted as a subscriber item
        var untagged = new List<long>();
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } },
            GetSubscriberAsyncFunc = (id, ct) =>
                Task.FromResult<Subscriber?>(new Subscriber { Id = id, EmailAddress = $"user{id}@test.com" }),
            UntagSubscriberAsyncFunc = (tagId, subId, ct) =>
            {
                untagged.Add(subId);
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        // Act
        var result = await TagCommands.HandleBulkRemove(["3", "42,43", "-y"], mockClient);

        // Assert
        result.Should().Be(0);
        untagged.Should().Equal([42L, 43L]);
        var output = writer.ToString();
        output.Should().Contain("Total: 2");
        output.Should().Contain("Removed: 2, Failed: 0");
    }

    [Fact]
    public async Task HandleBulkRemove_Should_Surface_Api_Failures()
    {
        var mockClient = new MockKitApiClient
        {
            Tags = new List<Tag> { new Tag { Id = 3, Name = "VIP" } },
            GetSubscriberAsyncFunc = (id, ct) =>
                Task.FromResult<Subscriber?>(new Subscriber { Id = id, EmailAddress = "user@test.com" }),
            UntagSubscriberAsyncFunc = (_, _, _) =>
                throw new HttpRequestException("Subscriber cannot be untagged")
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        var result = await TagCommands.HandleBulkRemove(["3", "42", "--force"], mockClient);

        result.Should().Be(1);
        writer.ToString().Should().Contain("Subscriber cannot be untagged");
    }

}
