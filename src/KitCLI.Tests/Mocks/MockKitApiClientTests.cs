using FluentAssertions;
using KitCLI.Models;
using KitCLI.Tests.TestData.Builders;

namespace KitCLI.Tests.Mocks;

/// <summary>
/// Tests for the MockKitApiClient and test data builders.
/// </summary>
public class MockKitApiClientTests
{
    [Fact]
    public async Task MockClient_Should_Return_Configured_Broadcasts()
    {
        // Arrange
        var broadcasts = BroadcastBuilder.BuildMany(3);
        var mockClient = new MockKitApiClient
        {
            Broadcasts = broadcasts.ToList()
        };

        // Act
        var response = await mockClient.GetBroadcastsAsync();

        // Assert
        response.Data.Should().HaveCount(3);
        response.Data[0].Subject.Should().Be("Test Broadcast 1");
    }

    [Fact]
    public async Task MockClient_Should_Return_Broadcast_By_Id()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(123)
            .WithSubject("Specific Broadcast")
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast }
        };

        // Act
        var result = await mockClient.GetBroadcastAsync(123);

        // Assert
        result.Should().NotBeNull();
        result!.Subject.Should().Be("Specific Broadcast");
    }

    [Fact]
    public async Task MockClient_Should_Return_Null_For_NonExistent_Broadcast()
    {
        // Arrange
        var mockClient = new MockKitApiClient();

        // Act
        var result = await mockClient.GetBroadcastAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MockClient_Should_Return_Configured_Stats()
    {
        // Arrange
        var stats = new BroadcastStatsBuilder()
            .WithRecipients(2500)
            .WithHighEngagement()
            .Build();

        var mockClient = new MockKitApiClient
        {
            BroadcastStats = new Dictionary<long, BroadcastStats> { [123] = stats }
        };

        // Act
        var result = await mockClient.GetBroadcastStatsAsync(123);

        // Assert
        result.Should().NotBeNull();
        result!.Recipients.Should().Be(2500);
        result.OpenRate.Should().BeApproximately(0.45, 0.01);
    }

    [Fact]
    public void BroadcastBuilder_Should_Create_Draft_Status()
    {
        // Arrange & Act
        var broadcast = new BroadcastBuilder()
            .AsDraft()
            .Build();

        // Assert
        broadcast.Status.Should().Be("draft");
        broadcast.SendAt.Should().BeNull();
    }

    [Fact]
    public void BroadcastBuilder_Should_Create_Sent_Status()
    {
        // Arrange & Act
        var broadcast = new BroadcastBuilder()
            .AsSent(DateTime.UtcNow.AddDays(-1))
            .Build();

        // Assert
        broadcast.Status.Should().Be("sent");
        broadcast.SendAt.Should().NotBeNull();
    }

    [Fact]
    public void BroadcastBuilder_Should_Create_Scheduled_Status()
    {
        // Arrange & Act
        var broadcast = new BroadcastBuilder()
            .AsScheduled(DateTime.UtcNow.AddDays(7))
            .Build();

        // Assert
        broadcast.Status.Should().Be("scheduled");
        broadcast.SendAt.Should().NotBeNull();
    }

    [Fact]
    public void BroadcastStatsBuilder_Should_Calculate_ClickToOpenRate()
    {
        // Arrange & Act
        // Kit V4 API provides EmailsOpened and TotalClicks
        // ClickToOpenRate = TotalClicks / EmailsOpened
        var stats = new BroadcastStatsBuilder()
            .WithEmailsOpened(400)
            .WithTotalClicks(100)
            .Build();

        // Assert
        stats.ClickToOpenRate.Should().BeApproximately(0.25, 0.01); // 100/400 = 25%
    }

    [Fact]
    public void BroadcastStatsBuilder_Should_Handle_Zero_Opens()
    {
        // Arrange & Act
        var stats = new BroadcastStatsBuilder()
            .WithNoEngagement()
            .Build();

        // Assert
        stats.ClickToOpenRate.Should().Be(0);
        stats.OpenRate.Should().Be(0);
        stats.ClickRate.Should().Be(0);
    }

    [Fact]
    public void BroadcastBuilder_BuildMany_Should_Create_Sequential_Broadcasts()
    {
        // Arrange & Act
        var broadcasts = BroadcastBuilder.BuildMany(5);

        // Assert
        broadcasts.Should().HaveCount(5);
        broadcasts[0].Id.Should().Be(1);
        broadcasts[4].Id.Should().Be(5);
        broadcasts.Select(b => b.Subject).Should().ContainInOrder(
            "Test Broadcast 1",
            "Test Broadcast 2",
            "Test Broadcast 3",
            "Test Broadcast 4",
            "Test Broadcast 5");
    }

    [Fact]
    public async Task MockClient_Should_Allow_Custom_Func_Override()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            GetBroadcastAsyncFunc = (id, ct) => Task.FromResult<Broadcast?>(
                new BroadcastBuilder()
                    .WithId(id)
                    .WithSubject($"Custom Broadcast {id}")
                    .Build())
        };

        // Act
        var result = await mockClient.GetBroadcastAsync(42);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Subject.Should().Be("Custom Broadcast 42");
    }

    [Fact]
    public async Task MockClient_Streaming_Should_Work()
    {
        // Arrange
        var subscribers = new List<Subscriber>
        {
            new() { Id = 1, EmailAddress = "one@test.com", State = "active" },
            new() { Id = 2, EmailAddress = "two@test.com", State = "active" },
            new() { Id = 3, EmailAddress = "three@test.com", State = "cancelled" }
        };

        var mockClient = new MockKitApiClient
        {
            Subscribers = subscribers
        };

        // Act
        var results = new List<Subscriber>();
        await foreach (var subscriber in mockClient.GetAllSubscribersAsync("active"))
        {
            results.Add(subscriber);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Select(s => s.EmailAddress).Should().Contain("one@test.com", "two@test.com");
    }
}
