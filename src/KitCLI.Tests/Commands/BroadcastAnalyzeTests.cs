using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;
using KitCLI.Tests.TestData.Builders;

namespace KitCLI.Tests.Commands;

/// <summary>
/// Tests for the broadcast analyze command.
/// Uses Console.SetOut for capturing output, so must not run in parallel with other console tests.
/// </summary>
[Collection("Console Output Tests")]
public class BroadcastAnalyzeTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public BroadcastAnalyzeTests()
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
    public async Task HandleAnalyze_Should_Return_Success_With_Valid_Broadcast()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(123)
            .WithSubject("Test Newsletter")
            .WithFromName("Test Sender")
            .WithFromEmail("sender@test.com")
            .AsSent(DateTime.UtcNow.AddDays(-1))
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithRecipients(2500)
            .WithEngagement(42, 12) // 42% open, 12% click - Kit V4 API uses percentages (0-100)
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [123] = stats }
        };

        // Capture console output
        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["123"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("Test Newsletter");
        output.Should().Contain("BROADCAST ANALYSIS");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Return_Error_For_NonExistent_Broadcast()
    {
        // Arrange
        var mockClient = new MockKitApiClient(); // Empty client

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["999"], mockClient);

        // Assert
        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Return_Error_For_Invalid_Id()
    {
        // Arrange
        var mockClient = new MockKitApiClient();

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["not-a-number"], mockClient);

        // Assert
        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("Invalid broadcast ID");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Return_Usage_With_No_Args()
    {
        // Arrange
        var mockClient = new MockKitApiClient();

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze([], mockClient);

        // Assert
        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("Usage:");
        output.Should().Contain("kit broadcast analyze");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Output_Json_Format()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(456)
            .WithSubject("JSON Test")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithTypicalEngagement()
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [456] = stats }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["456", "--format", "json"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("\"broadcastId\":");
        output.Should().Contain("\"subject\":");
        output.Should().Contain("JSON Test");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Handle_Zero_Engagement()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(789)
            .WithSubject("No Engagement Test")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithNoEngagement()
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [789] = stats }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["789"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("0.0%"); // Zero open rate
    }

    [Fact]
    public async Task HandleAnalyze_Should_Handle_High_Engagement()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(111)
            .WithSubject("High Engagement")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithHighEngagement()
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [111] = stats }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["111"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        // High engagement should show a positive rating
        output.Should().MatchRegex("(EXCELLENT|GREAT)");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Handle_Draft_Broadcast()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(222)
            .WithSubject("Draft Email")
            .AsDraft()
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast }
            // No stats for draft
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["222"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("DRAFT");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Show_Deliverability_When_Unsubscribes_Present()
    {
        // Arrange
        // Note: Kit V4 API only provides unsubscribes for deliverability metrics
        // (no bounces or complaints data available in V4)
        var broadcast = new BroadcastBuilder()
            .WithId(333)
            .WithSubject("Deliverability Test")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithRecipients(1000)
            .WithTypicalEngagement()
            .WithUnsubscribes(15)
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [333] = stats }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["333"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("DELIVERABILITY");
        output.Should().Contain("Unsubscribes");
        output.Should().Contain("15"); // The unsubscribe count
    }

    [Fact]
    public async Task HandleAnalyze_Should_Export_To_Csv()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(444)
            .WithSubject("Export Test")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithTypicalEngagement()
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [444] = stats }
        };

        var exportPath = Path.Combine(Path.GetTempPath(), $"test-export-{Guid.NewGuid()}.csv");

        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var result = await BroadcastCommands.HandleAnalyze(["444", "--export", exportPath], mockClient);

            // Assert
            result.Should().Be(0);
            File.Exists(exportPath).Should().BeTrue();

            var csvContent = await File.ReadAllTextAsync(exportPath);
            csvContent.Should().Contain("metric,value");
            csvContent.Should().Contain("broadcast_id,444");
            csvContent.Should().Contain("Export Test");
        }
        finally
        {
            // Cleanup
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    [Fact]
    public async Task HandleAnalyze_Should_Export_To_Json()
    {
        // Arrange
        var broadcast = new BroadcastBuilder()
            .WithId(555)
            .WithSubject("JSON Export Test")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithTypicalEngagement()
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [555] = stats }
        };

        var exportPath = Path.Combine(Path.GetTempPath(), $"test-export-{Guid.NewGuid()}.json");

        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var result = await BroadcastCommands.HandleAnalyze(["555", "--export", exportPath], mockClient);

            // Assert
            result.Should().Be(0);
            File.Exists(exportPath).Should().BeTrue();

            var jsonContent = await File.ReadAllTextAsync(exportPath);
            jsonContent.Should().Contain("\"broadcastId\"");
            jsonContent.Should().Contain("JSON Export Test");
        }
        finally
        {
            // Cleanup
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    /// <summary>
    /// Regression test for Issue #88: Broadcast stats shows impossible percentage values.
    /// Kit V4 API returns rates as percentages (0-100), not decimals (0-1).
    /// The bug was that code multiplied by 100, resulting in values like 3919% instead of 39.2%.
    /// </summary>
    [Fact]
    public async Task HandleAnalyze_Should_Display_Correct_Percentages_From_Api()
    {
        // Arrange
        // Simulate what the Kit V4 API actually returns: percentages in 0-100 format
        var broadcast = new BroadcastBuilder()
            .WithId(888)
            .WithSubject("Percentage Bug Test")
            .AsSent()
            .Build();

        var stats = new BroadcastStatsBuilder()
            .WithRecipients(1000)
            .WithOpenRate(39.19) // API returns 39.19 meaning 39.19%
            .WithClickRate(8.5)   // API returns 8.5 meaning 8.5%
            .WithEmailsOpened(420)
            .WithTotalClicks(85)
            .Build();

        var mockClient = new MockKitApiClient
        {
            Broadcasts = new List<Broadcast> { broadcast },
            BroadcastStats = new Dictionary<long, BroadcastStats> { [888] = stats }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await BroadcastCommands.HandleAnalyze(["888"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();

        // Key assertion: Output should contain "39.2%" not "3919%"
        output.Should().Contain("39.2%", "API returns 39.19 which means 39.19%, displayed as 39.2%");
        output.Should().NotContain("3919", "Bug #88: rates should not be multiplied by 100 again");

        // Also verify click rate is displayed correctly
        output.Should().Contain("8.5%", "API returns 8.5 which means 8.5%");
        output.Should().NotContain("850%", "Bug #88: click rate should not be multiplied by 100 again");
    }
}
